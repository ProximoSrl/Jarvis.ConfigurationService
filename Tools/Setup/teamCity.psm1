function Get-Artifact ($url,$Target,$username,$password) 
{ 
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
    $targetfile = New-Object IO.FileStream ($Target,[IO.FileMode]::Create) 
    [byte[]]$readbuffer = New-Object byte[] 8192 
    $progress = 0;
    # loop through the download stream and send the data to the target file 
    Write-Host "Downloading." 
    do{ 
        $readlength = $responsestream.Read($readbuffer,0,8192) 
        if ($readlength -lt 0) 
        {
            Write-Error "Unable to download file";
            return $false
        }
        $targetfile.Write($readbuffer,0,$readlength) 
        $progress = $progress + 1;
        if ($progress % 10 -eq 0) {Write-Host "." -NoNewline}
        if ($progress % 800 -eq 0) {Write-Host ""}
    } 
    while ($readlength -gt 0) 
    Write-Host "Downloaded $progress bytes."
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

