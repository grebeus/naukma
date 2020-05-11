page 50101 "Clustering Items"
{

    PageType = List;
    SourceTable = "Clustering Item";
    Caption = 'Clustering Items';
    ApplicationArea = All;
    UsageCategory = Lists;

    layout
    {
        area(content)
        {
            repeater(General)
            {
                field("Item No."; "Item No.")
                {
                    ApplicationArea = All;
                }
                field(Description; Description)
                {
                    ApplicationArea = All;
                }
                field("Item Category Code"; "Item Category Code")
                {
                    ApplicationArea = All;
                }
                field("Calculation Date"; "Calculation Date")
                {
                    ApplicationArea = All;
                }
                field("Cluster Assignments"; "Cluster Assignments")
                {
                    ApplicationArea = All;
                }
            }
        }
    }
    actions
    {
        area(Processing)
        {
            action("Process")
            {
                Caption = 'Process';
                Promoted = true;
                PromotedCategory = Process;
                PromotedIsBig = true;
                Image = Process;
                trigger OnAction()
                begin
                    ProcessSelectedRecords();
                    CurrPage.Update();
                    Message('Bingo!');
                end;
            }
        }
    }

    procedure ProcessSelectedRecords()
    var
        ClusteringItem: Record "Clustering Item";
        ClusteringMgt: Codeunit "Clustering Mgt.";
    begin
        CurrPage.SetSelectionFilter(ClusteringItem);
        ClusteringItem.MarkedOnly();
        if ClusteringItem.FindSet() then
            repeat
                ClusteringMgt.ProcessClusteringItem(ClusteringItem);
            until ClusteringItem.Next() = 0;
    end;
}
