page 50100 "Clustering Setup"
{

    PageType = Card;
    SourceTable = "Clustering Setup";
    Caption = 'Clustering Setup';
    ApplicationArea = All;
    UsageCategory = Administration;

    layout
    {
        area(content)
        {
            group(General)
            {
                field("API URI"; "API URI")
                {
                    ApplicationArea = All;
                }
                field("API Key"; "API Key")
                {
                    ApplicationArea = All;
                }
            }
        }
    }

    trigger OnOpenPage()
    begin
        Reset();
        if not Get() then begin
            Init();
            Insert();
        end;
    end;
}
