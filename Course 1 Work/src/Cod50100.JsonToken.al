codeunit 50100 "Json Token"
{
    var
        JTokenErr: Label 'Could not find a JsonToken with key %1';
        JPathErr: Label 'Could not find a token with path %1';

    procedure Get(JObject: JsonObject; JTokenKey: text) JToken: JsonToken
    begin
        if not JObject.Get(JTokenKey, JToken) then
            Error(JTokenErr, JTokenKey);

    end;

    procedure Select(JObject: JsonObject; JPath: text) JToken: JsonToken
    begin
        if not JObject.SelectToken(JPath, JToken) then
            Error(JPathErr, JPath);
    end;

    procedure Select(JTokenIn: JsonToken; JPath: text) JTokenOut: JsonToken
    begin
        if not JTokenIn.SelectToken(JPath, JTokenOut) then
            Error(JPathErr, JPath);
    end;
}
