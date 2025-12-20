namespace MapHive.Models.PageModels;

/// <summary>
/// Page model for the generic edit page
/// </summary>
public class EditItemPageModel
{
    /// <summary>
    /// Table name being edited
    /// </summary>
    public required string TableName { get; set; }

    /// <summary>
    /// ID of the item being edited
    /// </summary>
    public required int ItemId { get; set; }

    /// <summary>
    /// Column name to raw value mapping for display and editing
    /// Key is the actual column name (not display formatted)
    /// </summary>
    public required Dictionary<string, EditFieldInfo> Fields { get; set; }
}

/// <summary>
/// Information about an editable field
/// </summary>
public class EditFieldInfo
{
    /// <summary>
    /// The actual column name in the database
    /// </summary>
    public required string ColumnName { get; set; }

    /// <summary>
    /// Display-friendly name for the column
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Current value of the field
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Whether this field is the primary key (not editable)
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Whether this field is a foreign key
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// SQLite data type of the column
    /// </summary>
    public required string DataType { get; set; }

    /// <summary>
    /// Whether this field is nullable
    /// </summary>
    public bool IsNullable { get; set; }
}

