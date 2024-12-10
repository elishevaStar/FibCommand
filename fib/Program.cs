// See https://aka.ms/new-console-template for more information
//fib bundle --output bundleFile.txt  / D:folder/bundle/ bundleFile.txt
using System.CommandLine;

var languageOption = new Option<List<string>>(new[] { "--language", "-l" }, "List of programming languages to include (e.g., cs, java, py). Use 'all' to include all code files.")
{
    IsRequired = true // האופציה היא חובה
};
var bundleOption = new Option<FileInfo>(new[] { "--output", "-o" }, "File path and name");
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Include source file path as a comment in the bundle");
//) => "name" הוא ביטוי למתן ערך ברירת מחדל אם המשתמש לא בחר ימוין לפי שם הקובץ
var sortOption = new Option<string>(new[] { "--sort", "-s" }, () => "name", "Sort files by 'name' (default) or 'type' (file extension).");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Remove empty lines from the source code before bundling.");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "Name of the author to include as a comment at the top of the bundle file.");
var bundleCommand = new Command("bundle", "Bundle code files to a single file");
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(bundleOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((output, languages, includeNote, sort, removeEmptyLines, author) =>
{
    try
    {
        //רשימת סיומות מותרות
        var supportedExtensions = new[] { ".cs", ".java", ".py", ".txt", ".js", ".html", ".css" }; // רשימת סיומות מותרות

        // החרגת תיקיות לא רצויות
        var excludedFolders = new[] { "bin", "debug", "obj", ".git" };

        //  :השגת כל הקבצים עם כל הסיומות מהתיקיה הנוכחית
        //Directory.GetCurrentDirectory()-מחזירה את הנתיב של התיקיה הנוכחית
        //Directory.GetFiles-רשימה של כל הקבצים
        //"*.*"-כל הסיומות הקיימות 
        //SearchOption.AllDirectories-מבצע חיפוש רקורסיבי לא רק בתיקיה הראשית גם בתתי תיקיות 

        var currentDirectory = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories).Where(file =>
        !excludedFolders.Any(folder => file.Contains(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar)) &&
        supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
        .ToArray();

        files = sort.ToLower() switch
        {
            "type" => files.OrderBy(file => Path.GetExtension(file)).ThenBy(file => file).ToArray(),
            _ => files.OrderBy(file => Path.GetFileName(file)).ToArray() // ברירת מחדל לפי שם הקובץ
        };

        // :סינון קבצים לפי שפות
        //Path.GetExtension(file)-מחזירה את סיומת הקובץ
        //TrimStart('.').ToLower()-מסירה את הנקודה מסיומת הקובץ והופכת לאותיות קטנות
        // אח"כ בודקת האם הסיומת של הקובץ שנשלפה קודם נמצאת בתוך הרשימה languages

        files = files.Where(file =>
        {
            if (Path.GetFullPath(file) == Path.GetFullPath(output.FullName))
                return false; // דלג על הקובץ של הפלט עצמו

            if (languages.Contains("all"))
                return true;

            var extension = Path.GetExtension(file).TrimStart('.').ToLower();
            return languages.Contains(extension);
        }).ToArray();

        //מיון הקבצים לפי ערך sort
        //ממיין את המערך לפי סיומת הקובץ (Path.GetExtension(file)).
        //ThenBy:אם יש כמה קבצים עם אותה סיומת, ממיין אותם לפי השם המלא של הקובץ.
        //אם זה טייפ המיון לפי סוג הקוד 
        //אחרת _ דיפולט ממיין לפי שם א"ב של הקובץ 

        files = sort.ToLower() switch
        {
            "type" => files.OrderBy(file => Path.GetExtension(file)).ThenBy(file => file).ToArray(),
            _ => files.OrderBy(file => Path.GetFileName(file)).ToArray() // ברירת מחדל לפי שם הקובץ
        };
        Console.WriteLine($"Including files with extensions: {string.Join(", ", languages)}");
        // יצירת הקובץ לפלט

        using var writer = new StreamWriter(output.FullName);
        Console.WriteLine($"Creating bundle file at: {output.FullName}");

        // הוספת שם היוצר בראש הקובץ
        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
        }

        // כתיבת תוכן הקבצים לפלט:
        //File.ReadAllText-פונקציה שקוראת את כל התוכן של הקובץ שצוין בנתיב file 
        foreach (var file in files)
        {
            Console.WriteLine($"Adding file: {file}");
            var content = File.ReadAllText(file);

            // הסרת שורות ריקות אם נבחר
            if (removeEmptyLines)
            {
                //content.Split('\n'):מחלק את הקובץ לשורות
                //.Where(line => !string.IsNullOrWhiteSpace(line)):מסנן את השורות הריקות
                content = string.Join("\n", content.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)));

            }

            //כתיבת הערות מקור הקובץ
            if (includeNote)
            {
                //מציג את הנתיב היחסי
                var relativePath = Path.GetRelativePath(currentDirectory, file);
                writer.WriteLine($"// Source: {relativePath}");
            }
            writer.WriteLine($"// --- Start of {file} ---");
            writer.WriteLine(content);
            writer.WriteLine($"// --- End of {file} ---\n");
        }
        Console.WriteLine("Bundle completed successfully.");


    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: File path is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, bundleOption, languageOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

var createRspCommand = new Command("create-rsp", "Create a response file for bundling command");
createRspCommand.SetHandler(() =>
{
    Console.WriteLine("Creating a response file for the bundle command...");

    Console.Write("Enter output file name (e.g., output.bundle): ");
    var output = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("Error: Output file name cannot be empty.");
        return;
    }

    Console.Write("Enter programming languages (comma separated, e.g., cs, java, py, or 'all'): ");
    var languages = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(languages))
    {
        Console.WriteLine("Error: You must specify at least one programming language or 'all'.");
        return;
    }

    Console.Write("Remove empty lines? (yes/no): ");
    var removeEmptyLines = Console.ReadLine()?.ToLower() == "yes";

    Console.Write("Include source file path as comments? (yes/no): ");
    var includeNote = Console.ReadLine()?.ToLower() == "yes";

    Console.Write("Sort by (name/type): ");
    var sort = Console.ReadLine();
    if (sort != "name" && sort != "type")
    {
        Console.WriteLine("Error: Sort must be either 'name' or 'type'.");
        return;
    }

    Console.Write("Enter author name: ");
    var author = Console.ReadLine();

    // בניית הפקודה המלאה
    var rspContent = $"bundle --output {output} --language {languages} --sort {sort}";

    if (removeEmptyLines)
        rspContent += " --remove-empty-lines";

    if (includeNote)
        rspContent += " --note";

    if (!string.IsNullOrWhiteSpace(author))
        rspContent += $" --author \"{author}\"";

    // יצירת קובץ ה-rsp
    var rspFileName = "bundle-command.rsp";
    File.WriteAllText(rspFileName, rspContent);

    Console.WriteLine($"Response file created: {rspFileName}");
    Console.WriteLine("Run the command using: fib @bundle-command.rsp");
});

var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);
