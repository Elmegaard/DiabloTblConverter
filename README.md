# DiabloTblConverter
A simple app for converting .tbl files to .json and back again.

## Usage
To use it, run the tool with the follwoing parameters:
```
DiabloTblConverter.exe <target type [tbl|json]> <path to source file> <path to target file>
```

Example:
```powershell
# .tbl to .json
DiabloTblConverter.exe "json" "path\to\tbl_file.tbl" "path\to\json_file.json"

# .json to .tbl
DiabloTblConverter.exe "tbl" "path\to\json_file.json" "path\to\tbl_file.tbl"
```
