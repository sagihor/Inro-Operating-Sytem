
internal class Program
{
    class SpreadSheetThread
    {
        int nOperations;
        int nSleep;
        SharableSpreadSheet s;
        string ThreadName;
        private static readonly object randomLock = new object(); // shared by all threads
        private static readonly Random random = new Random();     // shared instance

        public SpreadSheetThread(int nOP, int nSleep, SharableSpreadSheet s, string tm)
        {
            this.nOperations = nOP;
            this.nSleep = nSleep;
            this.s = s;
            this.ThreadName = tm;
        }

        public void generateRandOperation()
        {
            Action[] methods = new Action[]
            {
        () => {
            try {
                var (rows, cols) = s.getSize();
                int row, col;
                lock (randomLock) {
                    row = random.Next(rows);
                    col = random.Next(cols);
                }
                s.setCell(row, col, "newValue");
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called setCell [{row}, {col}] -> newValue");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in setCell: {e.Message}");
            }
        },

        () => {
            try {
                var (rows, cols) = s.getSize();
                int row, col;
                lock (randomLock) {
                    row = random.Next(rows);
                    col = random.Next(cols);
                }
                string cell = s.getCell(row, col);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called getCell -> {cell}");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in getCell: {e.Message}");
            }
        },

        () => {
            try {
                var cell = s.searchString("testcell40");
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called searchString -> [{cell.Item1}, {cell.Item2}]");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in searchString: {e.Message}");
            }
        },

        () => {
            try {
                int r1, r2;
                lock (randomLock) {
                    var (rows, _) = s.getSize();
                    r1 = random.Next(rows);
                    r2 = random.Next(rows);
                }
                s.exchangeRows(r1, r2);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called exchangeRows -> {r1}, {r2} switched");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in exchangeRows: {e.Message}");
            }
        },

        () => {
            try {
                int c1, c2;
                lock (randomLock) {
                    var (_, cols) = s.getSize();
                    c1 = random.Next(cols);
                    c2 = random.Next(cols);
                }
                s.exchangeCols(c1, c2);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called exchangeCols -> {c1}, {c2} switched");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in exchangeCols: {e.Message}");
            }
        },

        () => {
            try {
                int row;
                lock (randomLock) {
                    var (rows, _) = s.getSize();
                    row = random.Next(rows);
                }
                s.addRow(row);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called addRow -> a row added after {row}");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in addRow: {e.Message}");
            }
        },

        () => {
            try {
                int col;
                lock (randomLock) {
                    var (_, cols) = s.getSize();
                    col = random.Next(cols);
                }
                s.addCol(col);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called addCol -> a col added after {col}");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in addCol: {e.Message}");
            }
        },

        () => {
            try {
                s.findAll("testcell00", true);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called findAll with caseSensitive = true and cell = testCell00");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in findAll: {e.Message}");
            }
        },

        () => {
            try {
                s.setAll("testcell00", "newValue", true);
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} called setAll with caseSensitive = true, switching testCell00 to newValue");
            } catch (Exception e) {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} error in setAll: {e.Message}");
            }
        }
            };

            try
            {
                int methodIndex;
                lock (randomLock)
                {
                    methodIndex = random.Next(methods.Length);
                }
               
                methods[methodIndex]();
               
            }
            catch (Exception e)
            {
                Console.WriteLine($"[{DateTime.Now}] {ThreadName} encountered error: {e.Message}");
            }
        }


        public void Run()
        {
            for (int i = 0; i < nOperations; i++)
            {
                generateRandOperation();
                Thread.Sleep(nSleep); // sleep for nSleep milliseconds between operations

            }
        }
    }

    private static void Main(string[] args)
    {

        if (args.Length != 5)
        {
            Console.WriteLine("Running command format: Simulator <rows> <cols> <nThreads> <nOperations> <mssleep>");
            return;
        }
        int rows = int.Parse(args[0]);
        int cols = int.Parse(args[1]);
        int nThreads = int.Parse(args[2]);
        int nOperations = int.Parse(args[3]);
        int nSleep = int.Parse(args[4]);



        SharableSpreadSheet sp = new SharableSpreadSheet(rows, cols, nThreads);
        sp.sheetSetUp();
        //the sheet before changes
        Console.WriteLine("Initial Spreadsheet:");
        sp.printSheet();
        SpreadSheetThread[] threads = new SpreadSheetThread[nThreads];
        Thread[] threadHandles = new Thread[nThreads];
        for (int i = 0; i < nThreads; i++)
        {
            string name = $"Thread {i}";
            threads[i] = new SpreadSheetThread(nOperations, nSleep, sp, name);
            threadHandles[i] = new Thread(new ThreadStart(threads[i].Run));  // להתחיל את התהליכון
            threadHandles[i].Start();
        }

        for (int i = 0; i < nThreads; i++)
        {
            threadHandles[i].Join();  // להמתין לסיום
        }
        //the sheet after changes
        Console.WriteLine("Final Spreadsheet:");
        sp.printSheet();
    }
}