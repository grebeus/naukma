codeunit 50101 "Clustering Mgt."
{
    var
        JsonToken: Codeunit "Json Token";

    procedure ProcessClusteringItem(var ClusteringItem: Record "Clustering Item")
    var
        RequestText: Text;
        ResponseText: Text;
    begin
        CreteRequest(ClusteringItem, RequestText);
        SendHttpPostRequest(RequestText, ResponseText);
        ProcessResponse(ResponseText, ClusteringItem);
    end;

    procedure CreteRequest(ClusteringItem: Record "Clustering Item"; var RequestText: Text);
    var
        CalculationDate: Date;
        YearMonth: Text;
    begin
        Clear(RequestText);

        if ClusteringItem."Calculation Date" <> 0D then
            CalculationDate := ClusteringItem."Calculation Date"
        else
            CalculationDate := Today();

        ClusteringItem.SetFilter(
            "Date Filter",
            StrSubstNo('%1..%2',
                CalcDate('-CM', CalculationDate),
                CalcDate('-CM+1M-1D', CalculationDate)));
        YearMonth := StrSubstNo('%1-%2',
            Date2DMY(CalculationDate, 3),
            PadStr('', 2 - StrLen(Format(Date2DMY(CalculationDate, 2))), '0') + Format(Date2DMY(CalculationDate, 2)));

        ClusteringItem.CalcFields("Cost (LCY)", "Sales (Qty)", "Sales (LCY)");
        RequestText := StrSubstNo('{"Inputs":{"history":{"ColumnNames":["Item No.","Month","Sales (Qty)","Sales (LCY)","Margin (LCY)"],"Values":[["%1","%2","%3","%4","%5"]]}},"GlobalParameters":{}}',
            ClusteringItem."Item No.",
            YearMonth,
            Format(ClusteringItem."Sales (Qty)", 0, 9),
            Format(ClusteringItem."Sales (LCY)", 0, 9),
            Format(ClusteringItem."Sales (LCY)" - ClusteringItem."Cost (LCY)", 0, 9));
    end;

    procedure SendHttpPostRequest(RequestText: Text; var ResponseText: Text)
    var
        ClusteringSetup: Record "Clustering Setup";
        HttpClient: HttpClient;
        HttpContent: HttpContent;
        HttpContHeader: HttpHeaders;
        HttpRequest: HttpRequestMessage;
        HttpReqHeader: HttpHeaders;
        HttpResponse: HttpResponseMessage;
        RequestURL: Text;
    begin
        ClusteringSetup.Get();
        RequestURL := ClusteringSetup."API URI";

        Clear(HttpContent);
        HttpContent.WriteFrom(RequestText);
        HttpContent.GetHeaders(HttpContHeader);
        HttpContHeader.Clear();
        HttpContHeader.Add('Content-Type', 'application/json');
        HttpContHeader.Add('Content-Length', Format(StrLen(RequestText)));

        Clear(HttpRequest);
        HttpRequest.GetHeaders(HttpReqHeader);
        HttpReqHeader.Clear();
        HttpReqHeader.Add('Authorization', StrSubstNo('Bearer %1', ClusteringSetup."API Key"));
        HttpRequest.Content(HttpContent);
        HttpRequest.SetRequestUri(RequestURL);
        HttpRequest.Method('POST');

        HttpClient.Send(HttpRequest, HttpResponse);
        HttpResponse.Content().ReadAs(ResponseText);
    end;

    procedure ProcessResponse(ResponseText: Text; var ClusteringItem: Record "Clustering Item")
    var
        JObject: JsonObject;
    begin
        if JObject.ReadFrom(ResponseText) then begin
            ClusteringItem."Cluster Assignments" := JsonToken.Select(JObject, '$.Results.clustering.value.Values[0][2]').AsValue().AsInteger();
            ClusteringItem.Modify();
        end;
    end;
}