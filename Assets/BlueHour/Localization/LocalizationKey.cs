using BlueHour.Core;

namespace BlueHour.Localization
{
    public static class LocalizationKey
    {
        public static string ForUi(string id)
        {
            return Create("ui", id);
        }

        public static string ForEducation(string id)
        {
            return Create("education", id);
        }

        private static string Create(string table, string id)
        {
            if (!DataId.IsValid(id))
                throw new System.ArgumentException("A localization key requires a valid ID.", nameof(id));

            return table + "." + id;
        }
    }
}
