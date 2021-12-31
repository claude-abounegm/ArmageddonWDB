# ArmageddonWDB

Proof of concept WDB Converter for legacy World of Warcraft (3.3.2). Compatible with ArcEmu.

## Introduction

This project was built in 2010 and has not been maintained since. I am open-sourcing it in hope to help others learn.

This project was split into 2 pieces:
- The WDB Converter which reads the `definitions.xml` file and parses the `.wdb` files accordingly. A `.sql` file is generated for each `.wdb` file.
- A MySQL Comparer which reads the generated `.sql` files, and compares the found data with the data in the database. In turn, it generates "patch"/"diff" `.sql` files which can executed as an incremental update for the DB. 

## Features
* Data structure and options could be configured through `definitions.xml`.
* Choose what should be shown in the console as an output, by adding to the column in XML `[output="true"]`.
* A logging system that logs everything that has been converted to `log.txt` based on what columns you chose as an output.
* Supports multiple `.wdb` files conversion. If you have multiple folders, each one has in it different wdb files, the converter will enter each folder and will convert them.

## Additional Notes

- This project was built with .NET Framework 3.5 on Windows. It can be built with .NET Core. However, `\r\n` are used as separators and `MySql.Data.dll` is used for DB operations. 
- For this project to be used, it would need to be converted to a new .NET Core solution and should use NuGet for the MySQL Connector package.
- I had used SVN for version control and pieces of the code take in considertion `.svn` directories for some reason, but they are not necessary and can be removed from code.