using System.Drawing;

namespace FluxAI.Desktop.Forms;

internal static class FluxAiTheme
{
    private static readonly Font BodyFont = new("Segoe UI", 9.5F);
    private static readonly Font ButtonFont = new("Segoe UI Semibold", 9.5F, FontStyle.Bold);
    private static readonly Font FieldLabelFont = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    private static readonly Font GroupHeadingFont = new("Segoe UI Semibold", 10F, FontStyle.Bold);
    private static readonly Font HeaderTitleFont = new("Segoe UI Semibold", 19F, FontStyle.Bold);
    private static readonly Font HeaderSubtitleFont = new("Segoe UI", 9.5F);
    private static readonly Font GridHeaderFont = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    private static readonly Font GridCellFont = new("Segoe UI", 9F);

    public static readonly Color Canvas = Color.FromArgb(243, 247, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Brand = Color.FromArgb(0, 64, 64);
    public static readonly Color Accent = Color.FromArgb(135, 206, 235);
    public static readonly Color Ink = Color.FromArgb(31, 48, 57);
    public static readonly Color Muted = Color.FromArgb(91, 109, 118);
    public static readonly Color Border = Color.FromArgb(218, 228, 232);
    public static readonly Color Selection = Color.FromArgb(219, 239, 246);
    public static readonly Color Sidebar = Color.FromArgb(21, 43, 48);
    public static readonly Color SidebarActive = Color.FromArgb(37, 76, 79);

    public static void Apply(Form form)
    {
        form.BackColor = Canvas;
        form.ForeColor = Ink;
        form.Font = BodyFont;
        ApplyControls(form.Controls);
    }

    public static void StylePrimaryButton(Button button)
    {
        if (!button.Enabled)
        {
            StyleDisabledPrimaryButton(button);
            return;
        }

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 85, 85);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 49, 49);
        button.BackColor = Brand;
        button.ForeColor = Color.White;
        button.Font = ButtonFont;
        button.Padding = new Padding(14, 7, 14, 7);
        button.Margin = new Padding(4, 4, 10, 4);
        button.Cursor = Cursors.Hand;
    }

    public static void StyleSecondaryButton(Button button)
    {
        if (!button.Enabled)
        {
            StyleDisabledSecondaryButton(button);
            return;
        }

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 243, 248);
        button.BackColor = Surface;
        button.ForeColor = Brand;
        button.Font = ButtonFont;
        button.Padding = new Padding(12, 6, 12, 6);
        button.Margin = new Padding(4, 4, 10, 4);
        button.Cursor = Cursors.Hand;
    }

    private static void StylePpoStepperButton(Button button, bool increase)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = increase
            ? Color.FromArgb(211, 237, 234)
            : Color.FromArgb(228, 239, 241);
        button.BackColor = increase ? Color.FromArgb(230, 245, 244) : Color.FromArgb(244, 248, 249);
        button.ForeColor = increase ? Brand : Muted;
        button.Font = new Font("Segoe UI Semibold", increase ? 12F : 13F, FontStyle.Bold);
        button.Padding = Padding.Empty;
        button.Margin = Padding.Empty;
        button.Cursor = Cursors.Hand;
    }

    private static void StyleDisabledSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.FromArgb(242, 246, 247);
        button.ForeColor = Color.FromArgb(151, 165, 170);
        button.Font = ButtonFont;
        button.Padding = new Padding(12, 6, 12, 6);
        button.Margin = new Padding(4, 4, 10, 4);
        button.Cursor = Cursors.Default;
    }

    public static void StyleDisabledPrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = Color.FromArgb(226, 233, 236);
        button.ForeColor = Muted;
        button.Font = ButtonFont;
        button.Padding = new Padding(14, 7, 14, 7);
        button.Margin = new Padding(4, 4, 10, 4);
        button.Cursor = Cursors.Default;
    }

    private static void ApplyControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case GroupBox group:
                    group.BackColor = Surface;
                    group.ForeColor = Brand;
                    group.Font = GroupHeadingFont;
                    group.Padding = new Padding(12);
                    break;
                case TabControl tabs:
                    tabs.BackColor = Surface;
                    tabs.ForeColor = Ink;
                    tabs.Font = ButtonFont;
                    tabs.Padding = new Point(14, 7);
                    break;
                case TabPage page:
                    page.BackColor = Canvas;
                    page.ForeColor = Ink;
                    page.Padding = new Padding(4);
                    break;
                case DataGridView grid:
                    StyleGrid(grid);
                    break;
                case Button button:
                    if (button.Tag?.ToString() == "ppo-stepper-minus")
                        StylePpoStepperButton(button, increase: false);
                    else if (button.Tag?.ToString() == "ppo-stepper-plus")
                        StylePpoStepperButton(button, increase: true);
                    else if (button.Tag?.ToString() == "primary")
                        StylePrimaryButton(button);
                    else if (button.Tag?.ToString() == "navigation")
                    {
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderSize = 0;
                        button.FlatAppearance.MouseOverBackColor = SidebarActive;
                        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(46, 89, 92);
                        button.UseVisualStyleBackColor = false;
                        button.BackColor = Sidebar;
                        button.ForeColor = Color.FromArgb(220, 233, 234);
                        button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                        button.Padding = new Padding(10, 0, 4, 0);
                    }
                    else
                        StyleSecondaryButton(button);
                    break;
                case CheckBox checkBox:
                    checkBox.FlatStyle = FlatStyle.Flat;
                    checkBox.ForeColor = Color.White;
                    checkBox.BackColor = Sidebar;
                    checkBox.Font = ButtonFont;
                    break;
                case TextBox textBox when textBox.Tag?.ToString() == "ppo-number-editor":
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Ink;
                    textBox.BorderStyle = BorderStyle.None;
                    textBox.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                    break;
                case TextBox textBox:
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Ink;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.Font = BodyFont;
                    break;
                case NumericUpDown number:
                    number.BackColor = Surface;
                    number.ForeColor = Ink;
                    number.Font = BodyFont;
                    break;
                case ComboBox combo:
                    combo.BackColor = Surface;
                    combo.ForeColor = Ink;
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.Font = BodyFont;
                    break;
                case Label label:
                    ApplyLabelStyle(label);
                    break;
                case TableLayoutPanel table:
                    table.BackColor = table.Tag?.ToString() switch
                    {
                        "canvas" => Canvas,
                        "brand" => Brand,
                        "sidebar" => Sidebar,
                        _ => Surface
                    };
                    break;
                case FlowLayoutPanel flow:
                    flow.BackColor = flow.Tag?.ToString() switch { "canvas" => Canvas, "sidebar" => Sidebar, _ => Surface };
                    break;
                case Panel panel when panel.Tag?.ToString() == "sidebar":
                    panel.BackColor = Sidebar;
                    break;
                case Panel panel when panel.Tag?.ToString() == "sidebar-separator":
                    panel.BackColor = Color.FromArgb(58, 80, 84);
                    break;
                case Panel panel when panel.Tag?.ToString() == "brand":
                    panel.BackColor = Brand;
                    break;
                case Panel panel:
                    panel.BackColor = panel.Tag?.ToString() == "canvas" ? Canvas : Surface;
                    break;
            }

            ApplyControls(control.Controls);
        }
    }

    private static void ApplyLabelStyle(Label label)
    {
        switch (label.Tag?.ToString())
        {
            case "inverse":
                label.ForeColor = Color.White;
                break;
            case "inverse-muted":
                label.ForeColor = Color.FromArgb(214, 231, 235);
                break;
            case "muted":
                label.ForeColor = Muted;
                break;
            case "status":
                break;
            case "field-label":
                label.ForeColor = Muted;
                label.Font = FieldLabelFont;
                break;
            case "header-title":
                label.ForeColor = Color.White;
                label.Font = HeaderTitleFont;
                break;
            case "page-title":
                label.ForeColor = Ink;
                label.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
                break;
            case "hero-title":
                label.ForeColor = Brand;
                label.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
                break;
            case "metric-value":
                label.ForeColor = Brand;
                label.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
                break;
            case "sidebar-brand":
                label.ForeColor = Color.White;
                label.Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold);
                break;
            case "sidebar-caption":
                label.ForeColor = Color.FromArgb(151, 176, 179);
                label.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
                break;
            case "navigation-outline":
                label.ForeColor = Ink;
                break;
            case "sidebar-caption-label":
                label.ForeColor = Color.FromArgb(151, 176, 179);
                break;
            case "sidebar-status":
                label.ForeColor = Color.FromArgb(229, 167, 151);
                break;
            case "demo-badge":
                label.ForeColor = Color.FromArgb(106, 73, 5);
                label.BackColor = Color.FromArgb(255, 239, 194);
                label.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
                label.Padding = new Padding(8, 5, 8, 5);
                break;
            case "header-subtitle":
                label.ForeColor = Color.FromArgb(214, 231, 235);
                label.Font = HeaderSubtitleFont;
                break;
            default:
                label.ForeColor = Ink;
                break;
        }
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Brand;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = GridHeaderFont;
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Ink;
        grid.DefaultCellStyle.SelectionBackColor = Selection;
        grid.DefaultCellStyle.SelectionForeColor = Ink;
        grid.DefaultCellStyle.Font = GridCellFont;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 251);
        grid.RowTemplate.Height = 30;
    }
}
