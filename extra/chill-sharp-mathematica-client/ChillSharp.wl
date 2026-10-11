BeginPackage["ChillSharp`"];

ChillSharpConnect::usage = "ChillSharpConnect[url, opts] creates a connection to a ChillSharp API. Options include \"ApiBasePath\", \"AccessToken\", \"Username\", and \"Password\".";
ChillSharpLogin::usage = "ChillSharpLogin[connection, username, password] authenticates a connection and stores its access and refresh tokens.";
ChillSharpClose::usage = "ChillSharpClose[connection] clears the local connection and its tokens.";
ChillSharpConnection::usage = "ChillSharpConnection is the opaque connection handle returned by ChillSharpConnect.";
ChillSharpQuery::usage = "ChillSharpQuery[connection, chillType, filters, opts] executes a server-side filtered, sorted, paginated query and returns a Dataset.";
ChillSharpFilter::usage = "ChillSharpFilter[operator, value] specifies a comparison operator for a property in the filters association. Supported operators include Equal, Contains, GreaterThan, Between, and In.";

Begin["`Private`"];

$connectionStore = <||>;
$filterOperators = {"Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Between", "Contains", "StartsWith", "EndsWith", "In", "IsNull", "IsNotNull", "IsEmpty", "IsNotEmpty"};

Options[ChillSharpConnect] = {
  "ApiBasePath" -> "api",
  "AccessToken" -> None,
  "Username" -> None,
  "Password" -> None
};

Options[ChillSharpQuery] = {
  "Page" -> 0,
  "PageResults" -> 100,
  "SortBy" -> "Position",
  "SortDirection" -> "ASC",
  "ResultProperties" -> Automatic,
  "LogicalOperator" -> "And"
};

ChillSharpConnect[url_String, OptionsPattern[]] := Module[
  {base, apiPath, id, username, password, token, connection, loginResult},
  base = StringTrim[url];
  If[base === "", Return[Failure["InvalidBaseURL", <|"Message" -> "The API URL cannot be empty."|>]]];

  apiPath = StringTrim[ToString[OptionValue["ApiBasePath"]], "/"];
  If[apiPath === "", apiPath = "api"];
  base = normalizeChillURL[base, apiPath];
  If[FailureQ[base], Return[base]];

  id = CreateUUID[];
  username = OptionValue["Username"];
  password = OptionValue["Password"];
  token = OptionValue["AccessToken"];
  AssociateTo[$connectionStore, id -> <|
    "ChillURL" -> base,
    "AccessToken" -> If[StringQ[token] && StringTrim[token] =!= "", StringTrim[token], None],
    "RefreshToken" -> None
  |>];
  connection = ChillSharpConnection[id];

  If[username =!= None || password =!= None,
    If[!StringQ[username] || !StringQ[password] || StringTrim[username] === "" || password === "",
      KeyDropFrom[$connectionStore, id];
      Return[Failure["InvalidCredentials", <|"Message" -> "Provide both a non-empty username and password."|>]]
    ];
    loginResult = ChillSharpLogin[connection, username, password];
    If[FailureQ[loginResult],
      KeyDropFrom[$connectionStore, id];
      Return[loginResult]
    ]
  ];

  connection
];

ChillSharpConnect[_, OptionsPattern[]] := Failure["InvalidBaseURL", <|"Message" -> "The API URL must be a string."|>];

ChillSharpLogin[connection_ChillSharpConnection, username_String, password_String] := Module[
  {id, state, response},
  id = connectionID[connection];
  state = getConnectionState[id];
  If[FailureQ[state], Return[state]];
  If[StringTrim[username] === "" || password === "", Return[Failure["InvalidCredentials", <|"Message" -> "Username and password are required."|>]]];

  response = requestOnce[
    id,
    serviceURL[state, "chill-auth"] <> "/login",
    <|"userNameOrEmail" -> StringTrim[username], "password" -> password|>,
    None
  ];
  If[FailureQ[response], Return[response]];
  If[!AssociationQ[response], Return[Failure["InvalidAuthResponse", <|"Message" -> "The login endpoint returned an invalid response."|>]]];
  response = storeTokenResponse[id, response];
  If[FailureQ[response], Return[response]];
  connection
];

ChillSharpLogin[_, _, _] := Failure["InvalidConnection", <|"Message" -> "Expected a ChillSharp connection, username, and password."|>];

ChillSharpClose[connection_ChillSharpConnection] := Module[{id = connectionID[connection]},
  If[StringQ[id], KeyDropFrom[$connectionStore, id]];
  Null
];

ChillSharpQuery[connection_ChillSharpConnection, chillType_String, filters_: <||>, OptionsPattern[]] := Module[
  {id, page, pageResults, sortBy, direction, logicalOperator, resultProperties, filterItems, payload, response, rows},
  id = connectionID[connection];
  If[FailureQ[getConnectionState[id]], Return[getConnectionState[id]]];
  If[StringTrim[chillType] === "", Return[Failure["InvalidChillType", <|"Message" -> "ChillType cannot be empty."|>]]];

  page = OptionValue["Page"];
  pageResults = OptionValue["PageResults"];
  sortBy = OptionValue["SortBy"];
  direction = ToUpperCase[ToString[OptionValue["SortDirection"]]];
  logicalOperator = ToString[OptionValue["LogicalOperator"]];
  resultProperties = OptionValue["ResultProperties"];

  If[!IntegerQ[page] || page < 0, Return[Failure["InvalidPage", <|"Message" -> "Page must be a non-negative, zero-based integer."|>]]];
  If[!IntegerQ[pageResults] || pageResults < 1, Return[Failure["InvalidPageResults", <|"Message" -> "PageResults must be a positive integer."|>]]];
  If[!StringQ[sortBy] || StringTrim[sortBy] === "", Return[Failure["InvalidSortProperty", <|"Message" -> "SortBy must name a ChillSharp property."|>]]];
  If[!MemberQ[{"ASC", "DESC"}, direction], Return[Failure["InvalidSortDirection", <|"Message" -> "SortDirection must be ASC or DESC."|>]]];
  If[!MemberQ[{"And", "Or"}, logicalOperator], Return[Failure["InvalidLogicalOperator", <|"Message" -> "LogicalOperator must be And or Or."|>]]];

  filterItems = buildFilterItems[filters];
  If[FailureQ[filterItems], Return[filterItems]];
  If[resultProperties =!= Automatic && (!ListQ[resultProperties] || !AllTrue[resultProperties, StringQ]),
    Return[Failure["InvalidResultProperties", <|"Message" -> "ResultProperties must be Automatic or a list of property names."|>]]
  ];

  payload = <|
    "chillType" -> StringTrim[chillType],
    "automaticQuery" -> <|"filter" -> <|
      "logicalOperator" -> logicalOperator,
      "filters" -> filterItems,
      "groups" -> {}
    |>|>,
    "properties" -> <||>,
    "resultProperties" -> If[resultProperties === Automatic, Null, (<|"name" -> #|> & /@ resultProperties)],
    "pagination" -> <|"page" -> page, "pageResults" -> pageResults|>,
    "ordering" -> <|"propertyName" -> StringTrim[sortBy], "direction" -> direction|>,
    "results" -> {}
  |>;

  response = sendJSON[connection, "query", payload];
  If[FailureQ[response], Return[response]];
  If[!AssociationQ[response], Return[Failure["InvalidQueryResponse", <|"Message" -> "The query endpoint returned an invalid response."|>]]];
  rows = lookupIgnoreCase[response, "results", {}];
  If[!ListQ[rows], Return[Failure["InvalidQueryResponse", <|"Message" -> "The query response does not contain a results list."|>]]];
  Dataset[normalizeEntity /@ rows]
];

ChillSharpQuery[_, _, ___] := Failure["InvalidQuery", <|"Message" -> "Expected a ChillSharp connection, ChillType string, and filters association."|>];

normalizeChillURL[url_String, apiPath_String] := Module[{lower = ToLowerCase[url], clean = url},
  clean = StringTrim[clean, "/"];
  lower = ToLowerCase[clean];
  Which[
    StringEndsQ[lower, "/chill"], clean,
    StringEndsQ[lower, "/chill-auth"], StringDrop[clean, -11] <> "/chill",
    StringEndsQ[lower, "/chill-schema"], StringDrop[clean, -13] <> "/chill",
    StringEndsQ[lower, "/chill-i18n"], StringDrop[clean, -11] <> "/chill",
    StringEndsQ[lower, "/chill-attachment"], StringDrop[clean, -17] <> "/chill",
    StringEndsQ[lower, "/" <> ToLowerCase[apiPath]], clean <> "/chill",
    True, clean <> "/" <> apiPath <> "/chill"
  ]
];

connectionID[ChillSharpConnection[id_String]] := id;
connectionID[_] := Missing["InvalidConnection"];

getConnectionState[id_String] := Lookup[$connectionStore, id, Failure["ConnectionClosed", <|"Message" -> "The connection is closed or invalid."|>]];
getConnectionState[_] := Failure["InvalidConnection", <|"Message" -> "Expected a ChillSharp connection."|>];

serviceURL[state_Association, service_String] := StringDrop[state["ChillURL"], -6] <> "/" <> service;

storeTokenResponse[id_String, response_Association] := Module[{state, access, refresh},
  state = getConnectionState[id];
  access = lookupIgnoreCase[response, "accessToken", None];
  refresh = lookupIgnoreCase[response, "refreshToken", None];
  If[!StringQ[access] || access === "", Return[Failure["InvalidAuthResponse", <|"Message" -> "The auth response did not contain an access token."|>]]];
  AssociateTo[$connectionStore, id -> Join[state, <|
    "AccessToken" -> access,
    "RefreshToken" -> If[StringQ[refresh] && refresh =!= "", refresh, None]
  |>]];
  Null
];

buildFilterItems[filters_Association] := Module[{rules = Normal[filters], items},
  items = Map[Function[rule, Module[{property, raw, operator, value, second = Null},
    property = ToString[First[rule]];
    raw = Last[rule];
    If[MatchQ[raw, _ChillSharpFilter],
      operator = raw[[1]];
      value = raw[[2]],
      operator = "Equal";
      value = raw
    ];
    If[!MemberQ[$filterOperators, operator], Return[Failure["InvalidFilterOperator", <|"Message" -> "Unsupported filter operator: " <> ToString[operator]|>]]];
    If[property === "", Return[Failure["InvalidFilterProperty", <|"Message" -> "Filter property names cannot be empty."|>]]];
    If[operator === "Between",
      If[!ListQ[value] || Length[value] =!= 2, Return[Failure["InvalidBetweenFilter", <|"Message" -> "Between filters require a two-item list."|>]]];
      second = value[[2]];
      value = value[[1]]
    ];
    <|"propertyName" -> property, "operator" -> operator, "value" -> value, "secondValue" -> second, "ignoreCase" -> False|>
  ]], rules];
  SelectFirst[items, FailureQ, items]
];

buildFilterItems[_] := Failure["InvalidFilters", <|"Message" -> "Filters must be an association from property names to values or ChillSharpFilter expressions."|>];

sendJSON[connection_ChillSharpConnection, path_String, payload_Association] := Module[
  {id = connectionID[connection], state, token, response, refresh},
  state = getConnectionState[id];
  If[FailureQ[state], Return[state]];
  token = state["AccessToken"];
  response = requestOnce[id, state["ChillURL"] <> "/" <> path, payload, token];
  If[FailureQ[response] && Lookup[response[[2]], "StatusCode", 0] === 401 && StringQ[state["RefreshToken"]] && state["RefreshToken"] =!= "",
    refresh = refreshConnection[id];
    If[!FailureQ[refresh],
      state = getConnectionState[id];
      response = requestOnce[id, state["ChillURL"] <> "/" <> path, payload, state["AccessToken"]]
    ]
  ];
  response
];

sendJSON[_, _, _] := Failure["InvalidRequest", <|"Message" -> "Could not send the request using this connection."|>];

refreshConnection[id_String] := Module[{state, refreshToken, response},
  state = getConnectionState[id];
  If[FailureQ[state], Return[state]];
  refreshToken = state["RefreshToken"];
  If[!StringQ[refreshToken] || refreshToken === "", Return[Failure["NoRefreshToken", <|"Message" -> "No refresh token is available; authenticate again with ChillSharpLogin."|>]]];
  response = requestOnce[id, serviceURL[state, "chill-auth"] <> "/refresh", <|"refreshToken" -> refreshToken|>, None];
  If[FailureQ[response], Return[response]];
  If[!AssociationQ[response], Return[Failure["InvalidAuthResponse", <|"Message" -> "The refresh endpoint returned an invalid response."|>]]];
  storeTokenResponse[id, response]
];

requestOnce[id_String, url_String, payload_Association, token_] := Module[
  {headers, requestOptions, request, response, status, body, parsed},
  headers = {"Accept" -> "application/json", "Content-Type" -> "application/json"};
  If[StringQ[token] && token =!= "", AppendTo[headers, "Authorization" -> "Bearer " <> token]];
  requestOptions = <|
    "Method" -> "POST",
    "Headers" -> headers,
    "Body" -> ExportString[payload, "JSON"]
  |>;
  request = HTTPRequest[url, requestOptions];
  response = Quiet[Check[URLRead[request], $Failed]];
  If[response === $Failed, Return[Failure["ConnectionFailed", <|"URL" -> url, "Message" -> "The HTTP request failed before receiving a response."|>]]];

  status = Quiet[Check[response["StatusCode"], 0]];
  body = Quiet[Check[response["Body"], ""]];
  If[Head[body] === ByteArray, body = FromCharacterCode[Normal[body], "UTF8"]];
  If[!StringQ[body], body = ToString[body, InputForm]];
  parsed = If[StringTrim[body] === "", Null, Quiet[Check[ImportString[body, "RawJSON"], $Failed]]];

  If[!IntegerQ[status] || status < 200 || status >= 300,
    Return[Failure["HTTPError", <|"URL" -> url, "StatusCode" -> status, "Body" -> body|>]]
  ];
  If[parsed === $Failed, Failure["InvalidJSONResponse", <|"URL" -> url, "StatusCode" -> status, "Body" -> body|>], parsed]
];

lookupIgnoreCase[association_Association, key_String, default_] := Module[{actual},
  actual = SelectFirst[Keys[association], ToLowerCase[ToString[#]] === ToLowerCase[key] &, Missing["NotFound"]];
  If[MissingQ[actual], default, association[actual]]
];

normalizeEntity[row_Association] := Module[{propertyKey, properties, topLevel},
  propertyKey = SelectFirst[Keys[row], ToLowerCase[ToString[#]] === "properties" &, Missing["NotFound"]];
  If[MissingQ[propertyKey], Return[row]];
  properties = row[propertyKey];
  topLevel = KeyDrop[row, {propertyKey}];
  If[AssociationQ[properties], Join[topLevel, properties], topLevel]
];

normalizeEntity[row_] := row;

End[];
EndPackage[];