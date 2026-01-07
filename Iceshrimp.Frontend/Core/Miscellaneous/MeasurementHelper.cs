namespace Iceshrimp.Frontend.Core.Miscellaneous;

public static class MeasurementHelper
{
    private static readonly string[] ByteOrders = ["B", "kB", "MB", "GB"];

    public static string BytesToHumanReadableSize(long value)
    {
        var order = 0;
        while (value >= 1000 && order < ByteOrders.Length - 1)
        {
            value /= 1000;
            order++;
        }

        return $"{value:N0} {ByteOrders[order]}";
    }
}
