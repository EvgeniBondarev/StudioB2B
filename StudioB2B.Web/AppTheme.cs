using MudBlazor;

namespace StudioB2B.Web;

public static class AppTheme
{
    public static MudTheme Light => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5C6BC0",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#78909C",
            SecondaryContrastText = "#FFFFFF",
            Background = "#F8F9FA",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1A1A2E",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1A1A2E",
            TextPrimary = "#1A1A2E",
            TextSecondary = "#6B7280",
            ActionDefault = "#6B7280",
            Divider = "#E5E7EB",
            TableLines = "#E5E7EB",
            Success = "#22C55E",
            Error = "#EF4444",
            Warning = "#F59E0B",
            Info = "#3B82F6",
            OverlayDark = "rgba(0,0,0,0.4)",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontSize = "0.875rem",
                LineHeight = "1.5",
            },
            H4 = new H4Typography { FontWeight = "600", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontWeight = "600" },
            H6 = new H6Typography { FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.9rem", FontWeight = "500" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "500" },
        },
        ZIndex = new ZIndex { Drawer = 1200, AppBar = 1100 },
    };

    public static MudTheme Dark => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#7986CB",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#90A4AE",
            SecondaryContrastText = "#FFFFFF",
            Background = "#0F1117",
            Surface = "#1A1D27",
            AppbarBackground = "#1A1D27",
            AppbarText = "#E5E7EB",
            DrawerBackground = "#1A1D27",
            DrawerText = "#E5E7EB",
            TextPrimary = "#E5E7EB",
            TextSecondary = "#9CA3AF",
            ActionDefault = "#9CA3AF",
            Divider = "#2D3748",
            TableLines = "#2D3748",
            Success = "#4ADE80",
            Error = "#F87171",
            Warning = "#FBBF24",
            Info = "#60A5FA",
            OverlayDark = "rgba(0,0,0,0.6)",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontSize = "0.875rem",
                LineHeight = "1.5",
            },
            H4 = new H4Typography { FontWeight = "600", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontWeight = "600" },
            H6 = new H6Typography { FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.9rem", FontWeight = "500" },
            Button = new ButtonTypography { TextTransform = "none", FontWeight = "500" },
        },
        ZIndex = new ZIndex { Drawer = 1200, AppBar = 1100 },
    };
}
