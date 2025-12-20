namespace MapHive.Models.PageModels;

/// <summary>
/// Page model for the generic create page
/// </summary>
public class CreateItemPageModel
{
    /// <summary>
    /// Table name for the new item
    /// </summary>
    public required string TableName { get; set; }

    /// <summary>
    /// Column name to field info mapping for creating new items
    /// Key is the actual column name (not display formatted)
    /// </summary>
    public required Dictionary<string, CreateFieldInfo> Fields { get; set; }
}

/// <summary>
/// Information about a creatable field
/// </summary>
public class CreateFieldInfo
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
    /// Default value of the field (if any)
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether this field is the primary key (auto-generated, not shown in form)
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

