namespace Dbosoft.OVN.Model;

public interface IOVSField
{
    public string GetColumnValue(string columnName, bool setMode);
    public string GetQueryString(string columnName, string option);
}