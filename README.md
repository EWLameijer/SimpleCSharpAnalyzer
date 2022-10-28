# Simple C# Analyzer

Counts lines in a C# folder (/solution/project).

Indicates lines that are too long (default 120 characters).

Note that you can change default warning sensisitivity by editing the settings.txt file.

Indicates when fields, method, local variables and properties are not capitalized according to [Microsoft Standards](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

Detects usage of String/Char/Int32 instead of string/char/int.

Also detects methods that are longer than Visser's ["Maintainable" software standards](https://www.softwareimprovementgroup.com/wp-content/uploads/Building_Maintainable_Software_C_Sharp_SIG.compressed.pdf) of 15 lines of code.

Detects when a method or constructor is not separated from previous declarations by a blank line.