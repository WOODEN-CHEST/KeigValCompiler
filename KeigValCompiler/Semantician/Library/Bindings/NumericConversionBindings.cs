namespace KeigValCompiler.Semantician.Library.Bindings;

/* Every conversion between the numeric types, each declared in the library on the type converted
 * from. Which ones are implicit is the language's rule, kept in one table here and written out in
 * agents/language.md as well: implicit where no value can be lost, explicit otherwise. */
internal class NumericConversionBindings : ILibraryBindingProvider
{
    // Static fields.
    private const string IMPLICIT = "I";
    private const string EXPLICIT = "E";
    private const string NONE = "-";

    /* The order of the table's rows, which are the types converted from, and of its columns, which are
     * the types converted to. */
    private static readonly LibraryType[] _numericTypes = new LibraryType[]
    {
        LibraryTypes.Int8, LibraryTypes.UInt8, LibraryTypes.Int16, LibraryTypes.UInt16, LibraryTypes.Int32,
        LibraryTypes.UInt32, LibraryTypes.Int64, LibraryTypes.UInt64, LibraryTypes.Char, LibraryTypes.Decimal
    };

    private static readonly string[] _conversionTable = new string[]
    {
        /*          Int8 UInt8 Int16 UInt16 Int32 UInt32 Int64 UInt64 Char Decimal */
        /* Int8    */ "-    E     I     E      I     E      I     E      E    I",
        /* UInt8   */ "E    -     I     I      I     I      I     I      E    I",
        /* Int16   */ "E    E     -     E      I     E      I     E      E    I",
        /* UInt16  */ "E    E     E     -      I     I      I     I      E    I",
        /* Int32   */ "E    E     E     E      -     E      I     E      E    E",
        /* UInt32  */ "E    E     E     E      E     -      I     I      E    E",
        /* Int64   */ "E    E     E     E      E     E      -     E      E    E",
        /* UInt64  */ "E    E     E     E      E     E      E     -      E    E",
        /* Char    */ "E    E     E     I      I     I      I     I      -    I",
        /* Decimal */ "E    E     E     E      E     E      E     E      E    -"
    };


    // Private methods.
    /* The table read into cells, checked for shape as it is: a malformed one is a mistake in the
     * compiler, so it throws. */
    private string[][] ReadTable()
    {
        if (_conversionTable.Length != _numericTypes.Length)
        {
            throw new InvalidOperationException($"The conversion table has {_conversionTable.Length} rows "
                + $"for {_numericTypes.Length} types.");
        }

        string[][] Cells = _conversionTable
            .Select(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();

        for (int Row = 0; Row < Cells.Length; Row++)
        {
            if (Cells[Row].Length != _numericTypes.Length)
            {
                throw new InvalidOperationException($"The conversion table's row for {_numericTypes[Row]} has "
                    + $"{Cells[Row].Length} cells for {_numericTypes.Length} types.");
            }
            for (int Column = 0; Column < Cells[Row].Length; Column++)
            {
                string Cell = Cells[Row][Column];
                bool IsKnownCell = (Cell == IMPLICIT) || (Cell == EXPLICIT) || (Cell == NONE);
                bool IsInItsPlace = (Cell == NONE) == (Row == Column);
                if (!IsKnownCell || !IsInItsPlace)
                {
                    throw new InvalidOperationException($"The conversion table's cell \"{Cell}\" from "
                        + $"{_numericTypes[Row]} to {_numericTypes[Column]} is not valid there.");
                }
            }
        }
        return Cells;
    }


    // Inherited methods.
    public void AddBindings(LibraryBindingTable table)
    {
        ArgumentNullException.ThrowIfNull(table, nameof(table));
        string[][] Cells = ReadTable();

        for (int Row = 0; Row < _numericTypes.Length; Row++)
        {
            SignatureBuilder Member = new(_numericTypes[Row]);
            for (int Column = 0; Column < _numericTypes.Length; Column++)
            {
                string Cell = Cells[Row][Column];
                if (Cell == NONE)
                {
                    continue;
                }
                table.AddIntrinsic(Member.Conversion(Cell == IMPLICIT, Member.Self, _numericTypes[Column]),
                    IntrinsicOperation.Convert);
            }
        }
    }
}
