$ErrorActionPreference = 'Stop'

$target = Join-Path $PSScriptRoot '..\wwwroot\uploads\cvs\demo'
$target = [System.IO.Path]::GetFullPath($target)
New-Item -ItemType Directory -Path $target -Force | Out-Null

$files = @(
    @{ Url = 'https://www.orimi.com/pdf-test.pdf'; Name = 'cv_amine_demo.pdf' },
    @{ Url = 'https://www.africau.edu/images/default/sample.pdf'; Name = 'cv_lina_demo.pdf' },
    @{ Url = 'https://www.clickdimensions.com/links/TestPDFfile.pdf'; Name = 'cv_sana_demo.pdf' }
)

foreach ($f in $files) {
    $dest = Join-Path $target $f.Name
    Invoke-WebRequest -Uri $f.Url -OutFile $dest
    Write-Host "Downloaded: $dest"
}

Write-Host 'Demo CV files ready in wwwroot/uploads/cvs/demo/'
