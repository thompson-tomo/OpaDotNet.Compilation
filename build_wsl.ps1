
$targets = @(
    @{
        OS = "windows";
        Arch= "amd64";
        Ext = "dll";
        CC = "x86_64-w64-mingw32-gcc";
        CXX = "x86_64-w64-mingw32-g++";
    },
    @{
        OS = "linux";
        Arch = "amd64";
        Ext = "so";
        Cc = "gcc";
        Cxx = "g++"
    }
)

if (Test-Path ./bin) {
    rm ./bin -Recurse
}

$targets | %{
    echo "Building $($_.OS)-$($_.Arch)"

    $env:CGO_ENABLED = 1
    $env:GOOS = $_.OS
    $env:GOARCH = $_.Arch
    $env:CC = $_.CC
    $env:CXX = $_.CXX

    $env:WSLENV = "GOOS/u:GOARCH/u:CGO_ENABLED/u:CC/u:CXX/u"

    wsl /usr/local/go/bin/go build -C ./interop -ldflags "-w -s" -buildmode=c-shared -o "../bin/Opa.Interop.$($_.Ext)" ./main.go
}

Write-Host -ForegroundColor Green "Done!"