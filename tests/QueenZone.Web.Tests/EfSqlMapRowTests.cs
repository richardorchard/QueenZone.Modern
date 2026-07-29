using System.Data;
using System.Data.Common;
using System.Reflection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// Exercises the cached property map path used by <c>EfSql.MapRow</c> (#400).
/// </summary>
public sealed class EfSqlMapRowTests
{
    [Fact]
    public void MapRow_maps_columns_case_insensitively_and_skips_nulls()
    {
        using var reader = new FakeDataReader(
            ["Name", "Count", "Optional"],
            [
                ["Freddie", 42, DBNull.Value],
                ["Brian", 7, "guitar"],
            ]);

        var mapRow = typeof(EfSql)
            .GetMethod("MapRow", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(SampleRow));

        Assert.True(reader.Read());
        var first = (SampleRow)mapRow.Invoke(null, [reader])!;
        Assert.Equal("Freddie", first.Name);
        Assert.Equal(42, first.Count);
        Assert.Null(first.Optional);

        Assert.True(reader.Read());
        var second = (SampleRow)mapRow.Invoke(null, [reader])!;
        Assert.Equal("Brian", second.Name);
        Assert.Equal(7, second.Count);
        Assert.Equal("guitar", second.Optional);
    }

    [Fact]
    public void MapRow_reuses_property_map_across_calls()
    {
        using var reader = new FakeDataReader(
            ["Name", "Count", "Optional"],
            [
                ["A", 1, "x"],
                ["B", 2, "y"],
            ]);

        var mapRow = typeof(EfSql)
            .GetMethod("MapRow", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(SampleRow));

        Assert.True(reader.Read());
        _ = mapRow.Invoke(null, [reader]);
        Assert.True(reader.Read());
        var second = (SampleRow)mapRow.Invoke(null, [reader])!;
        Assert.Equal("B", second.Name);
    }

    private sealed class SampleRow
    {
        public string Name { get; set; } = string.Empty;

        public int Count { get; set; }

        public string? Optional { get; set; }
    }

    private sealed class FakeDataReader : DbDataReader
    {
        private readonly string[] names;
        private readonly object[][] rows;
        private int index = -1;

        public FakeDataReader(string[] names, object[][] rows)
        {
            this.names = names;
            this.rows = rows;
        }

        public override int FieldCount => names.Length;

        public override bool HasRows => rows.Length > 0;

        public override bool IsClosed => false;

        public override int RecordsAffected => 0;

        public override int Depth => 0;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read()
        {
            index++;
            return index < rows.Length;
        }

        public override string GetName(int ordinal) => names[ordinal];

        public override int GetOrdinal(string name) => Array.FindIndex(names, n =>
            string.Equals(n, name, StringComparison.OrdinalIgnoreCase));

        public override object GetValue(int ordinal) => rows[index][ordinal];

        public override bool IsDBNull(int ordinal) => rows[index][ordinal] is DBNull or null;

        public override bool NextResult() => false;

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        public override DataTable GetSchemaTable() => throw new NotSupportedException();

        public override System.Collections.IEnumerator GetEnumerator() => throw new NotSupportedException();

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override char GetChar(int ordinal) => (char)GetValue(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

        public override Type GetFieldType(int ordinal) => GetValue(ordinal)?.GetType() ?? typeof(object);

        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);

        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

        public override string GetString(int ordinal) => (string)GetValue(ordinal);
    }
}
