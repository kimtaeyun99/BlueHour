namespace BlueHour.Core
{
    public static class DataId
    {
        public static bool IsValid(string value)
        {
            // IDs are stored as authored and do not change when display names change.
            return !string.IsNullOrWhiteSpace(value)
                && value == value.Trim();
        }
    }
}
