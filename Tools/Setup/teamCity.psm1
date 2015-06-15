function Get-ArtifactFromUrl
{ 
    param
    (
        [string] $url, 
        [string] $target, 
        [string] $username, 
        [string] $password
    )
    $authInfo = $username + ":" + $password
    $authInfo = [System.Convert]::ToBase64String([System.Text.Encoding]::Default.GetBytes($authInfo))

    $webRequest = [System.Net.WebRequest]::Create($url)
    $webRequest.Headers["Authorization"] = "Basic " + $authInfo
    $webRequest.PreAuthenticate = $true
    $webRequest.Timeout = 600000

    [System.Net.WebResponse] $resp = $webRequest.GetResponse()

    # get a download stream from the server response 
    $responsestream = $resp.GetResponseStream() 
    if (!$responsestream.CanRead)
    {
        return $false
    }
    # create the target file on the local system and the download buffer 
    $targetfile = New-Object IO.FileStream ($target,[IO.FileMode]::Create) 
    [byte[]]$readbuffer = New-Object byte[] 8192 
    $progress = 0;
    $size = 0;
    # loop through the download stream and send the data to the target file 
    Write-Host "Downloading." 
    do{ 
        $readlength = $responsestream.Read($readbuffer,0,8192) 
        if ($readlength -lt 0) 
        {
            Write-Error "Unable to download file";
            return $false
        }
        $size = $size + $readlength;
        $targetfile.Write($readbuffer,0,$readlength) 
        $progress = $progress + 1;
        if ($progress % 10 -eq 0) {Write-Host "." -NoNewline}
        if ($progress % 800 -eq 0) {Write-Host ""}
    } 
    while ($readlength -gt 0) 

    Write-Host ""
    Write-Host "Downloaded $size bytes."
    $targetfile.close() 
    return $true
} 

function Get-LatestBuildNumber   
{
    param
    (
        [string] $url,
        [string] $buildId,
        [string] $branch,
        [string] $username,
        [string] $password
    )
    $apiUrl = "http://$url/httpAuth/app/rest/builds/?locator=buildType:$buildId,branch:$branch,status:SUCCESS"
    Write-Host "URL: $apiUrl"
    $pair = "$($username):$($password)"

    $encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))

    $basicAuthValue = "Basic $encodedCreds"

    $Headers = @{
        Authorization = $basicAuthValue
    }

    $result = Invoke-WebRequest -Uri $apiUrl -Headers $Headers
    $xmlContent = [xml]$result.Content
    $latestBuildId= $xmlContent.SelectNodes("(/builds/build/@id)[1]")[0].Value

    return $latestBuildId

}

function Get-LatestBuildUri   
{
    param
    (
        [string] $url,
        [string] $buildId,
        [string] $latestBuildId
    )
   
    return "http://$url/repository/download/$buildId/$latestBuildId" + ":id"
}

function Get-ArtifactFromName
(
  [string] $teamCityUrl = "demo.prxm.it:8811",
  [string] $teamCityBuildId,
  [string] $artifactName,
  [string] $targetPath,
  [string] $branchName,
  [string] $user,
  [string] $plainPassword
)
{
    $lastBuildNumber = Get-LatestBuildNumber -url $teamCityUrl -buildId $teamCityBuildId -branch $branchName -username $user  -password $plainPassword
    $baseBuildUri = Get-LatestBuildUri -url $teamCityUrl -buildId $teamCityBuildId -latestBuildId $lastBuildNumber

    if ($baseBuildUri -eq $null) 
    {
        Write-Host "Error calling team city city server";
        return;
    }

    if(!(Test-Path -Path $targetPath))
    {
        $folderCreated = New-Item -ItemType directory -Path $targetPath
    }

    $fileName = [System.IO.Path]::GetFullPath($targetPath + "\" + $artifactName + ".build-$lastBuildNumber.zip") 
    $hostUri = "$baseBuildUri/$artifactName"
    if(Test-Path -Path $fileName)
    {
        Write-Host "Target File already downloaded: $fileName"
    }
    else
    {
        Write-Host "Download Host Url $hostUri on $fileName"

        $downloadResult = Get-ArtifactFromUrl -url $hostUri -target $fileName -username $user -password $plainPassword 
        $unblockResult = Unblock-File -Path $fileName
    }

    return $fileName
}

