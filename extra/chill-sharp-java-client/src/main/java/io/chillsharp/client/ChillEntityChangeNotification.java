package io.chillsharp.client;

import java.util.UUID;

/** A notification describing a changed Chill entity. */
public final class ChillEntityChangeNotification {
    private String chillType;
    private UUID guid;
    private String action;

    public String getChillType() { return chillType; }
    public void setChillType(String chillType) { this.chillType = chillType; }
    public UUID getGuid() { return guid; }
    public void setGuid(UUID guid) { this.guid = guid; }
    public String getAction() { return action; }
    public void setAction(String action) { this.action = action; }
}
