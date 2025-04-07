# Fast PowerShell script to list files not in .cursorignore
# Read .cursorignore file
$ignoreContent = Get-Content -Path ".cursorignore" -ErrorAction SilentlyContinue
$ignorePatterns = @()

foreach ($line in $ignoreContent) {
    # Skip comments and empty lines
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) {
        continue
    }
    
    # Add the pattern to our list
    $ignorePatterns += $line.Trim()
}

# Get all files
Write-Host "Processing files..."
$allFiles = Get-ChildItem -File -Recurse -Path . -ErrorAction SilentlyContinue

# Function to check if a path matches a glob pattern
function Test-GlobMatch {
    param (
        [string]$Path,
        [string]$Pattern
    )
    
    # Convert path separators for consistency
    $Path = $Path.Replace("\", "/")
    
    # Handle different pattern types
    if ($Pattern.EndsWith("/")) {
        # Directory pattern - check if the path is in this directory or a subdirectory
        $dirPattern = $Pattern.TrimEnd('/')
        
        # Handle **/ prefix for directory patterns
        if ($dirPattern.StartsWith("**/")) {
            $dirName = $dirPattern.Substring(3)
            # Check if path contains the directory at any level
            return $Path.Contains("/$dirName/") -or $Path.StartsWith("$dirName/") -or $Path.EndsWith("/$dirName")
        }
        
        return $Path.StartsWith("$dirPattern/") -or $Path -eq $dirPattern
    }
    elseif ($Pattern.StartsWith("**/")) {
        # Any directory depth pattern
        $suffix = $Pattern.Substring(3)
        # Check if path ends with the suffix or contains the suffix as a subdirectory path
        return $Path.EndsWith($suffix) -or $Path.Contains("/$suffix")
    }
    elseif ($Pattern -like "**/*.*") {
        # File extension pattern (e.g., **/*.db)
        $extension = $Pattern.Substring($Pattern.LastIndexOf("."))
        return $Path.EndsWith($extension)
    }
    elseif ($Pattern.Contains("**")) {
        # Create a more flexible regex pattern for ** patterns
        $patternParts = $Pattern.Split("**")
        $regexPattern = ""
        
        for ($i = 0; $i -lt $patternParts.Length; $i++) {
            $part = $patternParts[$i]
            $part = [regex]::Escape($part) -replace '\\\*', '[^/]*'
            
            if ($i -lt $patternParts.Length - 1) {
                $regexPattern += "$part.*"
            } else {
                $regexPattern += $part
            }
        }
        
        return $Path -match $regexPattern
    }
    elseif ($Pattern.Contains("*")) {
        # Simple wildcard pattern
        $regexPattern = [regex]::Escape($Pattern) -replace '\\\*', '[^/]*'
        return $Path -match "^$regexPattern$"
    }
    else {
        # Exact match
        return $Path -eq $Pattern
    }
}

# Filter files not in ignore patterns
$includedFiles = @()
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Substring($PWD.Path.Length + 1).Replace("\", "/")
    $shouldInclude = $true
    
    # Check against all ignore patterns
    foreach ($pattern in $ignorePatterns) {
        if (Test-GlobMatch -Path $relativePath -Pattern $pattern) {
            $shouldInclude = $false
            break
        }
    }
    
    if ($shouldInclude) {
        $includedFiles += $relativePath
    }
}

# Write output
"" | Out-File -FilePath "output.txt" -Encoding utf8
foreach ($file in $includedFiles) {
    "@$file" | Add-Content -Path "output.txt" -Encoding utf8
}

Write-Host "Done! Check output.txt for results." 