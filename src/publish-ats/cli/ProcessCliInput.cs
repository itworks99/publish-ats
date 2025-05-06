namespace publish_ats.cli;

internal static class ProcessCliInput
{
    private static readonly string[] FileTypesArray = ["pdf", "docx", "doc", "md"];

    internal static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            switch (arg)
            {
                case "-i" or "--input" when i + 1 < args.Length:
                    options.InputFilePath = args[++i];
                    break;
                case "-o" or "--output" when i + 1 < args.Length:
                    options.OutputFilePath = args[++i];
                    break;
                case "-f" or "--format" when i + 1 < args.Length:
                {
                    foreach (var format in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var fileType = format.Trim().ToLower();
                        if (FileTypesArray.Contains(fileType))
                            options.OutputFormats.Add(fileType);
                        else
                            Console.WriteLine($"Warning: Ignoring unsupported format '{format}'");
                    }

                    break;
                }
                case "-a" or "--ats":
                    options.OptimizeForAts = true;
                    break;
                case "-h" or "--help":
                    Help.PrintToConsole();
                    break;
                default:
                {
                    if (i == 0 && !arg.StartsWith("-"))
                    {
                        options.InputFilePath = args[i];
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Unknown option '{args[i]}'");
                    }

                    break;
                }
            }
        }

        return options;
    }
}