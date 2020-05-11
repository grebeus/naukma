table 50101 "Clustering Item"
{
    Caption = 'Clustering Item';

    fields
    {
        field(1; "Item No."; Code[20])
        {
            Caption = 'Item No.';
            DataClassification = CustomerContent;
            TableRelation = Item;
            ValidateTableRelation = false;
        }
        field(2; "Calculation Date"; Date)
        {
            Caption = 'Calculation Date';
            DataClassification = CustomerContent;
        }
        field(3; Description; Text[100])
        {
            Caption = 'Description';
            FieldClass = FlowField;
            CalcFormula = lookup (Item.Description where ("No." = field ("Item No.")));
            Editable = false;
        }
        field(4; "Item Category Code"; Code[20])
        {
            Caption = 'Item Category Code';
            FieldClass = FlowField;
            CalcFormula = lookup (Item."Item Category Code" where ("No." = field ("Item No.")));
            Editable = false;
        }
        field(10; "Date Filter"; Date)
        {
            Caption = 'Date Filter';
            DataClassification = CustomerContent;
        }
        field(11; "Sales (Qty)"; Decimal)
        {
            Caption = 'Sales (Qty)';
            FieldClass = FlowField;
            CalcFormula = - sum ("Item Ledger Entry".Quantity where ("Entry Type" = filter (Sale), "Item No." = field ("Item No.")));
        }

        field(12; "Sales (LCY)"; Decimal)
        {
            Caption = 'Sales (LCY)';
            FieldClass = FlowField;
            CalcFormula = sum ("Value Entry"."Sales Amount (Actual)" where ("Item Ledger Entry Type" = filter (Sale), "Item No." = field ("Item No.")));
        }
        field(13; "Cost (LCY)"; Decimal)
        {
            Caption = 'Cost (LCY)';
            FieldClass = FlowField;
            CalcFormula = sum ("Value Entry"."Cost Amount (Actual)" where ("Item Ledger Entry Type" = filter (Sale), "Item No." = field ("Item No.")));
        }
        field(20; "Cluster Assignments"; Integer)
        {
            Caption = 'Cluster Assignments';
            DataClassification = CustomerContent;
        }
    }
    keys
    {
        key(PK; "Item No.")
        {
            Clustered = true;
        }
    }

}
