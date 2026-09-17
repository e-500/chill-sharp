namespace ChillSharp.Dto
{
    /// <summary>
    /// Frontend-oriented abstraction of CLR property types.
    /// Numeric values are stable and can be safely consumed by clients.
    /// </summary>
    public enum ChillDtoPropertyType
    {
        Unknown = 0,
        Guid = 1,
        Integer = 10,
        Decimal = 20,
        Date = 30,
        Time = 40,
        DateTime = 50,
        Duration = 60,
        Boolean = 70,
        String = 80,
        Text = 81,
        Json = 99,
        ChillEntity = 1000,
        ChillEntityCollection = 1010,
        ChillQuery = 1100
    }
}