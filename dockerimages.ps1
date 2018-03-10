$images = docker images | Select-String "local/caketest" -AllMatches
if($images.Matches.Count -eq 2)
{
    Write-Host "Docker images found in local registry."
    exit 0
}
else 
{
    Write-Error "No Docker images found in local registry."
    exit 1
}