namespace InvestmentHub.Domain.Enums;

/// <summary>
/// Types of Polish Treasury Bonds (Obligacje Skarbowe).
/// </summary>
public enum BondType
{
    /// <summary>3-month bonds (Oszczędnościowe Trzymiesięczne)</summary>
    OTS,

    /// <summary>1-year bonds (Roczne Oszczędnościowe)</summary>
    ROR,

    /// <summary>2-year bonds (Dwuletnie Oszczędnościowe Rodzinne)</summary>
    DOR,

    /// <summary>3-year bonds (Trzyletnie Oszczędnościowe)</summary>
    TOS,

    /// <summary>4-year bonds indexed to inflation (Czteroletnie Oszczędnościowe Indeksowane)</summary>
    COI,

    /// <summary>6-year bonds (Rodzinne Oszczędnościowe Sześcioletnie)</summary>
    ROS,

    /// <summary>10-year bonds (Emerytalne Dziesięcioletnie Oszczędnościowe)</summary>
    EDO,

    /// <summary>12-year bonds (Rodzinne Oszczędnościowe Dwunastoletnie)</summary>
    ROD
}
