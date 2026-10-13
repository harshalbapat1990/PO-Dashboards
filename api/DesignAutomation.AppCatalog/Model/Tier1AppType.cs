namespace DesignAutomation.AppCatalog.Model;

/// <summary>Enums the various Tier 1 apps for deserialization. A project can use more than one.</summary>
[Flags]
public enum Tier1AppType
{
    /// <summary></summary>
	None = 0,
    /// <summary></summary>
    APS = 1,
    /// <summary></summary>
    ProjectWise = 2,
    /// <summary></summary>
    Esri = 4,
    /// <summary></summary>
    Revizto = 8,
    /// <summary></summary>
    BlueBeam = 16
}