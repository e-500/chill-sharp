import { CommonModule } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { forkJoin, map, Observable, of, switchMap } from 'rxjs';
import { PermissionAction, PermissionEffect, PermissionScope } from '../../models/chill-auth.models';
import { ChillService } from '../../services/chill.service';
import type { PermissionEditorRow } from './permission-editor.component';

type PermissionNodeKind = 'module' | 'group' | 'entity' | 'property' | 'query' | 'command';
export type PermissionCellState = 'unchecked' | 'allow' | 'deny' | 'inherited-allow' | 'inherited-deny';

interface PermissionTreeNode {
  id: string;
  label: string;
  kind: PermissionNodeKind;
  module: string;
  scope?: PermissionScope;
  entityName?: string;
  propertyName?: string;
  children: PermissionTreeNode[];
}

interface VisiblePermissionNode {
  node: PermissionTreeNode;
  depth: number;
}

interface PermissionColumn {
  action: PermissionAction;
  label: string;
}

interface ResolvedCellRule {
  rule: PermissionEditorRow;
  inherited: boolean;
  subjectPriority: number;
}

const PERMISSION_COLUMNS: PermissionColumn[] = [
  { action: PermissionAction.FullControl, label: 'FullControl' },
  { action: PermissionAction.Command, label: 'Command' },
  { action: PermissionAction.Query, label: 'Query' },
  { action: PermissionAction.Create, label: 'Create' },
  { action: PermissionAction.Update, label: 'Update' },
  { action: PermissionAction.Delete, label: 'Delete' },
  { action: PermissionAction.See, label: 'See' },
  { action: PermissionAction.Modify, label: 'Modify' }
];

@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './permission-matrix.component.html',
  styleUrl: './permission-matrix.component.scss'
})
export class PermissionMatrixComponent {
  readonly chill = inject(ChillService);

  readonly rows = input<PermissionEditorRow[]>([]);
  readonly inheritedRows = input<PermissionEditorRow[]>([]);
  readonly rowsChange = output<PermissionEditorRow[]>();

  readonly columns = PERMISSION_COLUMNS;
  readonly tree = signal<PermissionTreeNode[]>([]);
  readonly expandedNodeIds = signal<string[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal('');

  readonly visibleRows = computed<VisiblePermissionNode[]>(() => {
    const expanded = new Set(this.expandedNodeIds());
    const visible: VisiblePermissionNode[] = [];

    const append = (nodes: PermissionTreeNode[], depth: number): void => {
      for (const node of nodes) {
        visible.push({ node, depth });
        if (node.children.length > 0 && expanded.has(node.id)) {
          append(node.children, depth + 1);
        }
      }
    };

    append(this.tree(), 0);
    return visible;
  });

  constructor() {
    this.loadTree();
  }

  toggleExpanded(nodeId: string): void {
    this.expandedNodeIds.update((ids) =>
      ids.includes(nodeId) ? ids.filter((id) => id !== nodeId) : [...ids, nodeId]
    );
  }

  isExpanded(nodeId: string): boolean {
    return this.expandedNodeIds().includes(nodeId);
  }

  isActionAvailable(node: PermissionTreeNode, action: PermissionAction): boolean {
    if (node.kind === 'group') {
      return false;
    }

    if (action === PermissionAction.FullControl) {
      return true;
    }

    switch (node.kind) {
      case 'module':
        return true;
      case 'entity':
        return [PermissionAction.Query, PermissionAction.Create, PermissionAction.Update, PermissionAction.Delete].includes(action);
      case 'property':
        return action === PermissionAction.See || action === PermissionAction.Modify;
      case 'query':
        return action === PermissionAction.Query;
      case 'command':
        return action === PermissionAction.Command;
      default:
        return false;
    }
  }

  cellState(node: PermissionTreeNode, action: PermissionAction): PermissionCellState {
    if (!this.isActionAvailable(node, action)) {
      return 'unchecked';
    }

    const resolved = this.resolveRule(node, action);
    if (!resolved) {
      return 'unchecked';
    }

    const allowed = resolved.rule.effect === PermissionEffect.Allow;
    if (resolved.inherited) {
      return allowed ? 'inherited-allow' : 'inherited-deny';
    }

    return allowed ? 'allow' : 'deny';
  }

  cellLabel(node: PermissionTreeNode, column: PermissionColumn): string {
    const state = this.cellState(node, column.action);
    const status = {
      unchecked: 'Unchecked',
      allow: 'Allow',
      deny: 'Deny',
      'inherited-allow': 'Inherited allow',
      'inherited-deny': 'Inherited deny'
    }[state];
    return `${node.label}, ${column.label}: ${status}`;
  }

  togglePermission(node: PermissionTreeNode, action: PermissionAction): void {
    if (!this.isActionAvailable(node, action) || !node.scope) {
      return;
    }

    const currentRows = this.rows();
    const existingIndex = currentRows.findIndex((row) => this.isExactRule(row, node, action));
    if (existingIndex >= 0 && currentRows[existingIndex].effect === PermissionEffect.Deny) {
      this.rowsChange.emit(currentRows.filter((_, index) => index !== existingIndex));
      return;
    }

    const nextEffect = existingIndex >= 0
      ? PermissionEffect.Deny
      : PermissionEffect.Allow;
    const nextRow: PermissionEditorRow = {
      localId: existingIndex >= 0 ? currentRows[existingIndex].localId : `matrix-${crypto.randomUUID()}`,
      guid: existingIndex >= 0 ? currentRows[existingIndex].guid : '',
      effect: nextEffect,
      action,
      scope: node.scope,
      module: node.module,
      entityName: node.entityName ?? '',
      propertyName: node.propertyName ?? '',
      appliesToAllProperties: false,
      description: existingIndex >= 0 ? currentRows[existingIndex].description : ''
    };

    const nextRows = existingIndex >= 0
      ? currentRows.map((row, index) => index === existingIndex ? nextRow : row)
      : [...currentRows, nextRow];
    this.rowsChange.emit(nextRows);
  }

  private loadTree(): void {
    this.isLoading.set(true);
    this.chill.getModuleList().pipe(
      map((modules) => [...modules].sort((left, right) => left.localeCompare(right))),
      switchMap((modules) => modules.length
        ? forkJoin(modules.map((module) => this.loadModuleNode(module)))
        : of([]))
    ).subscribe({
      next: (nodes) => {
        this.tree.set(nodes);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.chill.formatError(error));
        this.isLoading.set(false);
      }
    });
  }

  private loadModuleNode(module: string): Observable<PermissionTreeNode> {
    return forkJoin({
      entities: this.chill.getEntityList(module),
      queries: this.chill.getQueryList(module),
      commands: this.chill.getCommandList(module)
    }).pipe(
      switchMap(({ entities, queries, commands }) => {
        const entityRequests = [...entities].sort((left, right) => left.localeCompare(right))
          .map((entity) => this.loadEntityNode(module, entity));

        return forkJoin(entityRequests.length ? entityRequests : [of<PermissionTreeNode[]>([])]).pipe(
          map((entityResults) => {
            const entityNodes = entityResults.flat();
            return {
              id: `module:${module}`,
              label: module,
              kind: 'module' as const,
              module,
              scope: PermissionScope.Module,
              children: [
                this.groupNode(module, 'entities', 'Entities', entityNodes),
                this.groupNode(module, 'queries', 'Queries', [...queries].sort((left, right) => left.localeCompare(right)).map((query) => ({
                  id: `query:${module}:${query}`,
                  label: query,
                  kind: 'query' as const,
                  module,
                  scope: PermissionScope.Entity,
                  entityName: query,
                  children: []
                }))),
                this.groupNode(module, 'commands', 'Commands', [...commands].sort((left, right) => left.localeCompare(right)).map((command) => ({
                  id: `command:${module}:${command}`,
                  label: command,
                  kind: 'command' as const,
                  module,
                  scope: PermissionScope.Entity,
                  entityName: command,
                  children: []
                })))
              ]
            };
          })
        );
      })
    );
  }

  private loadEntityNode(module: string, entity: string): Observable<PermissionTreeNode[]> {
    return this.chill.getPropertyList(entity).pipe(
      map((properties) => [{
        id: `entity:${module}:${entity}`,
        label: entity,
        kind: 'entity' as const,
        module,
        scope: PermissionScope.Entity,
        entityName: entity,
        children: [...properties].sort((left, right) => left.localeCompare(right)).map((property) => ({
          id: `property:${module}:${entity}:${property}`,
          label: property,
          kind: 'property' as const,
          module,
          scope: PermissionScope.Property,
          entityName: entity,
          propertyName: property,
          children: []
        }))
      }])
    );
  }

  private groupNode(module: string, groupId: string, label: string, children: PermissionTreeNode[]): PermissionTreeNode {
    return {
      id: `group:${module}:${groupId}`,
      label,
      kind: 'group',
      module,
      children
    };
  }

  private resolveRule(node: PermissionTreeNode, action: PermissionAction): ResolvedCellRule | null {
    const candidates = [
      ...this.rows().filter((rule) => this.ruleApplies(rule, node, action)).map((rule) => ({
        rule,
        inherited: rule.scope < (node.scope ?? rule.scope),
        subjectPriority: 0
      })),
      ...this.inheritedRows().filter((rule) => this.ruleApplies(rule, node, action)).map((rule) => ({
        rule,
        inherited: true,
        subjectPriority: 1
      }))
    ];

    candidates.sort((left, right) =>
      left.subjectPriority - right.subjectPriority
      || right.rule.scope - left.rule.scope
      || Number(left.rule.action !== action) - Number(right.rule.action !== action)
      || Number(right.rule.effect === PermissionEffect.Deny) - Number(left.rule.effect === PermissionEffect.Deny)
    );

    return candidates[0] ?? null;
  }

  private ruleApplies(rule: PermissionEditorRow, node: PermissionTreeNode, action: PermissionAction): boolean {
    if (rule.action !== action && !(action !== PermissionAction.FullControl && rule.action === PermissionAction.FullControl)) {
      return false;
    }

    if (!this.sameName(rule.module, node.module)) {
      return false;
    }

    if (rule.scope === PermissionScope.Module) {
      return true;
    }

    if (rule.scope === PermissionScope.Entity) {
      const targetEntity = node.kind === 'property' ? node.entityName : node.entityName;
      return !!targetEntity && this.sameName(rule.entityName, targetEntity);
    }

    return node.kind === 'property'
      && this.sameName(rule.entityName, node.entityName)
      && (rule.appliesToAllProperties || this.sameName(rule.propertyName, node.propertyName));
  }

  private isExactRule(rule: PermissionEditorRow, node: PermissionTreeNode, action: PermissionAction): boolean {
    return rule.action === action
      && rule.scope === node.scope
      && this.sameName(rule.module, node.module)
      && (rule.scope === PermissionScope.Module
        || this.sameName(rule.entityName, node.entityName))
      && (rule.scope !== PermissionScope.Property
        || (!rule.appliesToAllProperties && this.sameName(rule.propertyName, node.propertyName)));
  }

  private sameName(left: string | undefined, right: string | undefined): boolean {
    return (left?.trim() ?? '').toLocaleLowerCase() === (right?.trim() ?? '').toLocaleLowerCase();
  }
}