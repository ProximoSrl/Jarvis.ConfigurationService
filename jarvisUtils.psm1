#taken from here https://gist.github.com/jpoehls/2726969
#edit xmlnodes with xpath
function Edit-XmlNodes {
param (
    [xml] $doc = $(throw "doc is a required parameter"),
    [string] $xpath = $(throw "xpath is a required parameter"),
    [string] $value = $(throw "value is a required parameter"),
    [bool] $condition = $true
)    
    if ($condition -eq $true) {
        $nodes = $doc.SelectNodes($xpath)
         
        foreach ($node in $nodes) {
            if ($node -ne $null) {
                if ($node.NodeType -eq "Element") {
                    $node.InnerXml = $value
                }
                else {
                    $node.Value = $value
                }
            }
        }
    }
}

function Get-YesNoAnswer(
    [string] $question,
    [string] $default = '') 
{
    do 
    {
        Write-Host $question -NoNewline
        $answer = Read-Host
        if ($answer -eq '') 
        {
            $answer = $default
        }
        $answer = $answer.ToLower()
    } while ($answer -ne 'y' -and $answer -ne 'n')

    return $answer;
}

function Get-StringAnswer(
    [string] $question,
    [string] $defaultValue) 
{
    Write-Host $question -NoNewline
    $answer = Read-Host
    if ($answer -eq '') 
    {
       return $defaultValue
    }

    return $answer
}

function New-Queue(
    [string] $queueName
)
{
    $queueName = "intranet.dms.input"
    $queue = Get-MsmqQueue –Name $queueName –QueueType Private
    if ($queue -eq $null)
    {
        Write-Host "Creating msmq $queueName"
        New-MsmqQueue –Name $queueName –QueueType Private
    }
}

function Expand-WithShell(
    [string] $zipFile,
    [string] $destinationFolder,
    [bool] $deleteOld = $true,
    [bool] $quietMode = $false) 
{
    $shell = new-object -com shell.application

    if ((Test-Path $destinationFolder) -and $deleteOld)
    {
          Remove-Item $destinationFolder -Recurse -Force
    }

    New-Item $destinationFolder -ItemType directory

    $zip = $shell.NameSpace($zipFile)
    foreach($item in $zip.items())
    {
        if (!$quietMode) { Write-Host "unzipping " + $item.Name }
        $shell.Namespace($destinationFolder).copyhere($item)
    }
}
