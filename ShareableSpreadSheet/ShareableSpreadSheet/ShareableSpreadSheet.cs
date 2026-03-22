using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
/**
 * ShareableSpreadSheet.cs
 * This file contains the implementation of a sharable spreadsheet with reader‑writer locks for concurrency control.
 * The spreadsheet allows multiple readers to read simultaneously while ensuring exclusive access for writers.
 */
class readersWriterMutex
{
    private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

    public void readerEnter() => rwLock.EnterReadLock();
    public void readerExit() => rwLock.ExitReadLock();
    public void writerEnter() => rwLock.EnterWriteLock();
    public void writerExit() => rwLock.ExitWriteLock();
}

public class SharableSpreadSheet
{
    int k;
    int nUsers;
    int nRows;
    int nCols;
    string[,] sheet;
    readersWriterMutex[] mutexes;
    readersWriterMutex structure_mutex;

    public SharableSpreadSheet(int nRows, int nCols, int nUsers = -1)
    {
        this.nRows = nRows;
        this.nCols = nCols;
        this.nUsers = nUsers;

        k = Environment.ProcessorCount;
        sheet = new string[nRows, nCols];
        mutexes = new readersWriterMutex[k];
        structure_mutex = new readersWriterMutex();
        for (int i = 0; i < k; i++) mutexes[i] = new readersWriterMutex();
    }

    public int mapFunc (int row)
    {
        return (int) (row / k);
    }

    private void validateRow(int row)
    {
        if (row < 0 || row >= nRows)
            throw new ArgumentOutOfRangeException(nameof(row), "Row index is out of range.");
    }
    private void validateCol(int col)
    {
        if (col < 0 || col >= nCols)
            throw new ArgumentOutOfRangeException(nameof(col), "Column index is out of range.");
    }

    public void lockStructureWriter()
    {
        for (int i = 0; i < k; i++)
        {
            mutexes[i].writerEnter();
        }
    }

    public void unlockStructureWriter()
    {
        for (int i = k; i > 0; i--)
        {
            mutexes[i - 1].writerExit();
        }
    }

    public void lockStructureReader()
    {
        for (int i = 0; i < k; i++)
        {
            mutexes[i].readerEnter();
        }
    }

    public void unlockStructureReader()
    {
        for (int i = k; i > 0; i--)
        {
            mutexes[i - 1].readerExit();
        }
    }

    public string getCell(int row, int col)
    {
        validateRow(row);
        validateCol(col);
        mutexes[mapFunc(row)].readerEnter();
        try { return sheet[row, col]; }
        finally { mutexes[mapFunc(row)].readerExit(); }
    }

    public void setCell(int row, int col, string str)
    {
        validateRow(row);
        validateCol(col);
        mutexes[mapFunc(row)].writerEnter();
        try { sheet[row, col] = str; }
        finally { mutexes[mapFunc(row)].writerExit(); }
    }

    public Tuple<int, int> searchString(string str)
    {
        for (int i = 0; i < nRows; i++)
        {
            mutexes[mapFunc(i)].readerEnter();
            try
            {
                for (int j = 0; j < nCols; j++)
                    if (sheet[i, j] != null && sheet[i, j].Equals(str))
                        return Tuple.Create(i, j);
            }
            finally { mutexes[mapFunc(i)].readerExit(); }
        }
        return Tuple.Create(-1, -1);
    }

    public int searchInRow(int row, string str)
    {
        validateRow(row);
        mutexes[mapFunc(row)].readerEnter();
        try
        {
            for (int j = 0; j < nCols; j++)
                if (sheet[row, j] != null && sheet[row, j].Equals(str))
                    return j;
            return -1;
        }
        finally { mutexes[mapFunc(row)].readerExit(); }
    }

    public int searchInCol(int col, string str)
    {
        validateCol(col);
        for (int i = 0; i < nRows; i++)
        {
            mutexes[mapFunc(i)].readerEnter();
            try { if (sheet[i, col] != null && sheet[i, col].Equals(str)) return i; }
            finally { mutexes[mapFunc(i)].readerExit(); }
        }
        return -1;
    }

    public Tuple<int, int> searchInRange(int col1, int col2, int row1, int row2, string str)
    {
        validateRow(row1); validateRow(row2);
        validateCol(col1); validateCol(col2);
        for (int i = row1; i <= row2; i++)
        {
            mutexes[mapFunc(i)].readerEnter();
            try
            {
                for (int j = col1; j <= col2; j++)
                    if (sheet[i, j] != null && sheet[i, j].Equals(str))
                        return Tuple.Create(i, j);
            }
            finally { mutexes[mapFunc(i)].readerExit(); }
        }
        return Tuple.Create(-1, -1);
    }

    public Tuple<int, int>[] findAll(string str, bool caseSensitive)
    {
        List<Tuple<int, int>> res = new List<Tuple<int, int>>();
        for (int i = 0; i < nRows; i++)
        {
            mutexes[mapFunc(i)].readerEnter();
            try
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (sheet[i, j] == null) continue;
                    bool match = caseSensitive ? sheet[i, j].Equals(str)
                                                : sheet[i, j].Equals(str, StringComparison.OrdinalIgnoreCase);
                    if (match) res.Add(Tuple.Create(i, j));
                }
            }
            finally { mutexes[mapFunc(i)].readerExit(); }
        }
        return res.ToArray();
    }

    public void setAll(string oldStr, string newStr, bool caseSensitive)
    {
        for (int i = 0; i < nRows; i++)
        {
            mutexes[mapFunc(i)].writerEnter();
            try
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (sheet[i, j] == null) continue;
                    bool match = caseSensitive ? sheet[i, j].Equals(oldStr)
                                                : sheet[i, j].Equals(oldStr, StringComparison.OrdinalIgnoreCase);
                    if (match) sheet[i, j] = newStr;
                }
            }
            finally { mutexes[mapFunc(i)].writerExit(); }
        }
    }

    public void exchangeRows(int row1, int row2)
    {
        if (row1 == row2) return; // No need to exchange the same row
        validateRow(row1);
        validateRow(row2);
        int mapRow1 = mapFunc(row1);
        int mapRow2 = mapFunc(row2);

        if (mapRow1 == mapRow2)
        {
            // Both rows are in the same partition, we can lock directly
            mutexes[mapRow1].writerEnter();
            try
            {
                for (int col = 0; col < nCols; col++)
                {
                    string tmp = sheet[row1, col];
                    sheet[row1, col] = sheet[row2, col];
                    sheet[row2, col] = tmp;
                }
            }
            finally { mutexes[mapRow1].writerExit(); }
            return;
        }
        else
        {
            int minMap = Math.Min(mapRow1, mapRow2);
            int maxMap = Math.Max(mapRow1, mapRow2);

            mutexes[minMap].writerEnter();
            mutexes[maxMap].writerEnter();

            try
            {
                for (int col = 0; col < nCols; col++)
                {
                    string tmp = sheet[row1, col];
                    sheet[row1, col] = sheet[row2, col];
                    sheet[row2, col] = tmp;
                }
            }
            finally
            {
                mutexes[maxMap].writerExit();
                mutexes[minMap].writerExit();
            }

        }

    }

    public void exchangeCols(int col1, int col2)
    {
        if (col1 == col2) return; // No need to exchange the same column
        validateCol(col1);
        validateCol(col2);

        lockStructureWriter();
        try
        {
            for (int row = 0; row < nRows; row++)
            {


                string tmp = sheet[row, col1];
                sheet[row, col1] = sheet[row, col2];
                sheet[row, col2] = tmp;
            }
        }
        finally
        {
            unlockStructureWriter();
        }
    }

    public void addRow(int row)
    {
        validateRow(row);
        lockStructureWriter();
        try
        {
            var newSheet = new string[nRows + 1, nCols];
            for (int i = 0; i <= row; i++)
                for (int j = 0; j < nCols; j++)
                    newSheet[i, j] = sheet[i, j];
            for (int i = row + 1; i <= nRows; i++)
                for (int j = 0; j < nCols; j++)
                    newSheet[i, j] = sheet[i - 1, j];
            sheet = newSheet;
            nRows++;
        }
        finally { unlockStructureWriter(); }
    }

    public void addCol(int col)
    {
        validateCol(col);
        lockStructureWriter();
        try
        {
            var newSheet = new string[nRows, nCols + 1];
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j <= col; j++)
                    newSheet[i, j] = sheet[i, j];
                for (int j = col + 1; j <= nCols; j++)
                    newSheet[i, j] = sheet[i, j - 1];
            }
            sheet = newSheet;
            nCols++;
        }
        finally { unlockStructureWriter(); }
    }

    public Tuple<int, int> getSize()
    {
        lockStructureReader();
        try { return Tuple.Create(nRows, nCols); }
        finally { unlockStructureReader(); }
    }

    public void sheetSetUp()
    {
        for (int i = 0; i < nRows; i++)
            for (int j = 0; j < nCols; j++)
                sheet[i, j] = $"testCell{i}{j}";
    }

    public void printSheet()
    {
        for (int i = 0; i < nRows; i++)
        {
            for (int j = 0; j < nCols; j++) Console.Write(sheet[i, j] + "\t");
            Console.WriteLine();
        }
    }

    public void save(String fileName)
    {
        structure_mutex.readerEnter();
        try
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine(nRows);
                writer.WriteLine(nCols);
                for (int i = 0; i < nRows; i++)
                {
                    for (int j = 0; j < nCols; j++)
                    {
                        writer.Write(sheet[i, j]);
                        if (j < nCols - 1) writer.Write("\t");
                    }
                    writer.WriteLine();
                }
            }
        }
        finally
        {
            structure_mutex.readerExit();
        }
    }

    public void load(String fileName)
    {
        structure_mutex.writerEnter();
        try
        {
            string[] lines = File.ReadAllLines(fileName);
            nRows = int.Parse(lines[0]);
            nCols = int.Parse(lines[1]);
            sheet = new string[nRows, nCols];
            for (int i = 0; i < nRows; i++)
            {
                string[] parts = lines[i + 2].Split('\t');
                for (int j = 0; j < nCols; j++)
                {
                    sheet[i, j] = parts[j];
                }
            }
        }
        finally
        {
            structure_mutex.writerExit();
        }
    }

    public List<List<string>> ToList()
 {
        var result = new List<List<string>>();
        for (int i = 0; i < nRows; i++)
        {
            var row = new List<string>();
            for (int j = 0; j < nCols; j++)
            {
                row.Add(sheet[i, j]);
            }
            result.Add(row);
        }
        return result;
    }
}
