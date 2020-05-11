table 50100 "Clustering Setup"
{
    Caption = 'Clustering Setup';

    fields
    {
        field(1; "Primary Key"; Code[20])
        {
            Caption = 'Primary Key';
            DataClassification = CustomerContent;
        }
        field(2; "API URI"; Text[512])
        {
            Caption = 'API URI';
            DataClassification = CustomerContent;
            ExtendedDatatype = URL;
        }
        field(3; "API Key"; Text[512])
        {
            Caption = 'API Key';
            DataClassification = CustomerContent;
            ExtendedDatatype = Masked;
        }
    }
    keys
    {
        key(PK; "Primary Key")
        {
            Clustered = true;
        }
    }

}
