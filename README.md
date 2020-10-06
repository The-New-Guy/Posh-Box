# Posh-Box

## Build Status
| Branch | Status |
|--------|--------|
|**Master**|[![Build Status](https://dev.azure.com/The-New-Guy/Hobbies/_apis/build/status/Posh-Box?branchName=master)](https://dev.azure.com/The-New-Guy/Hobbies/_build/latest?definitionId=10&branchName=master)|
|**NuGet**|[![NuGet Deployment Status](https://dev.azure.com/The-New-Guy/Hobbies/_apis/build/status/Posh-Box?branchName=master&stageName=Deploy&jobName=NuGet%20Packages)](https://dev.azure.com/The-New-Guy/Hobbies/_build/latest?definitionId=10&branchName=master)
|**GitHub**|[![GitHub Release](https://dev.azure.com/The-New-Guy/Hobbies/_apis/build/status/Posh-Box?branchName=master&stageName=Deploy&jobName=GitHub%20Release)](https://dev.azure.com/The-New-Guy/Hobbies/_build/latest?definitionId=10&branchName=master)

This is a work in progress. This README file currently a place to take notes more than anything.

## How to Install Module

> Module publishing not implemented yet.

Either download the module from the [Releases](Releases) page or build the module using the instructions in the [How to Build](#how-to-build) section.

Once you have acquired the module place it in your PSModulePath. Read more about PSModulePath [Here](https://msdn.microsoft.com/en-us/library/dd878324%28v=vs.85%29.aspx).

``` powershell
Write-Host $env:PSModulePath
```

## How to Import Module

From the root of the project folder, after building run the following:

```powershell
# Once the module is installed in your PSModulePath.
Import-Module Posh-Box

# If you want to run it from the project folder after building it.
Import-Module .\src\Posh-Box.psd1
```

## Configuration and Authentication

The configuration file is a text file containing the administrative OAuth information provided by the Box administration panel for a given registered application. It will look something like this (but with less fake looking data, mostly long strings of letters and numbers):

```
{
  "boxAppSettings": {
    "clientID": "someLongIdString",
    "clientSecret": "noneOfMyBusinessWhatSecretsTheClientHas",
    "appAuth": {
      "publicKeyID": "someIdString",
      "privateKey": "-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIFDsuperSecurePrivateKey\nWic=\n-----END ENCRYPTED PRIVATE KEY-----\n",
      "passphrase": "superSecurePassphrase"
    }
  },
  "enterpriseID": "IdNumber"
}

```

After you have the module imported and the configuration file placed in the desired location run the following to load the OAuth information and authenticate to Box:

```powershell
Connect-Box -ConfigPath C:\Path\To\Config\config.json
```

You can work directly with the newly generated Box client by retrieving it with the following command:

```powershell
$client = Get-BoxClient
```

## How to Build

### Prerequisites

To build the project you need to have .NET Core SDK installed as well as the dotnet.exe CLI command installed. For details see this link I haven't added to this page yet.

### Build Project

From the root of the project folder run the following:

```powershell
# To build the project.
dotnet build -v m -o src/lib

# To test the project.
dotnet test

# To clean the project.
dotnet clean -v m -o src/lib
```

## Notes on Rate Limiting

Box will sometimes rate limits API requests and return a response of `429 Too Many Requests`. For most API calls this will be 1000 requests per minute, per user. For other specific rate limits see [here](https://developer.box.com/guides/api-calls/permissions-and-errors/rate-limits/).

Below are a few notes to keep in mind if you are running into rate limit issues:

* In this module, most commands that return multiple items will have a `-PageSize` parameter that can be used to increase the number of items per page, thus decreasing the number of potential API requests, up to a maximum of 1000 items per page.

* Many commands have parameters that will filter down the number of results returned, thus reducing the need to make additional API calls to get all the relevant results.

* Using the `-Verbose` flag on any command that makes an API call will print a verbose message for every API call made to Box. Allowing you to better analyze any rate limiting issues you might be having.

## Examples

### Get Child Items of a Folder

```powershell
# Get child items of the given folder with default properties.
Get-BoxChildItem -UserID 123456789 -ItemID 123456789012

Name                                         Id           Type  Path
----                                         --           ----  ----
MyCoolStuffYouWouldntBelieve1            457530729       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve2            365590557       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve3            794235317       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve4            316971799       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve5            335770552       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve6           21233563674      folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve7           141300702887     folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve8            998236544       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve9           77951312460      folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve0           90553780990      folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve11           444070189       folder /path/to/folder/with/id/of/123456789012
MyCoolStuffYouWouldntBelieve.txt         5426425482       file  /path/to/folder/with/id/of/123456789012

# Get child items of the given folder with all properties.
Get-BoxChildItem -UserID 123456789 -ItemID 123456789012 -Properties *

# Get child items of the given folder with custom properties.
Get-BoxChildItem -UserID 123456789 -ItemID 123456789012 -Properties @('Id', 'Name', 'Size', 'ModifiedAt')

# Get child items of the given folder with default properties plus custom properties.
Get-BoxChildItem -UserID 123456789 -ItemID 123456789012 -Properties ([PoshBox.Helper.PropertyNameCompleter]::DefaultPropertyNames + @('Size', 'ModifiedAt'))
```

### Get Child Items of a Folder Recursively

```powershell
# Recur infinitely.
$items = Get-BoxChildItem -UserID 123456789 -ItemID 123456789012 -Recurse

# Only recur into immediate sub folders.
$items = Get-BoxChildItem -UserID 123456789 -ItemID 123456789012 -Recurse -RecursionDepth 1
```

### Get Item Information

The `Get-BoxItem` command supports most of the same parameters as the previous `Get-BoxChildItem` command examples.

```powershell
# Get item information for given Box item ID.
$item = Get-BoxItem -UserID 123456789 -ItemID 123456789012

# Get item information for given Box File ID. This can save an API call by explicitly stating the item type.
$items = Get-BoxItem -UserID 123456789 -ItemID 123456789012 -ItemType File
```

### Search for Items

There are many ways to search for items on Box. Here are just a few:

```powershell
# Search for items with any of the words "New", "Text", "Document" in the default content fields (Name and Description).
# Search is limited to items the UserID has access to.
Search-Box 'New Text Document' -UserID 123456789

# Search for items with "New Text Document" in the default content fields (Name and Description).
Search-Box '"New Text Document"' -UserID 123456789

# Make 3 separate searches for items with any of the words "New", "Text", "Document" in the default content fields (Name and Description).
'New', 'Text', 'Document' | Search-Box -UserID 123456789

# Search for items with the word "CoolAwesomeFile" in the default content fields (Name and Description).
# Search is limited to items under the AncestorFolders location recursively.
Search-Box 'CoolAwesomeFile' -UserID 123456789 -AncestorFolders 123456789012

# Search for items with the word "CoolAwesomeSharedFile" in the default content fields (Name and Description).
# Search is limited to items the UserID has access to and to items owned by OwnerID.
Search-Box 'CoolAwesomeSharedFile' -UserID 123456789 -OwnerID 987654321

# Search for items with the word "Backup" in the default content fields (Name and Description).
# Search is limited to items over 1GB in size.
Search-Box 'Backup' -UserID 123456789 -SizeLowerBound 1GB

# Search for items with the word "CoolAwesomeFile" in the default content fields (Name and Description).
# Search is limited to items created after 12/21/2012 12:00:00 AM.
Search-Box 'CoolAwesomeFile' -UserID 123456789 -CreatedAfter '12/21/2012 12:00:00 AM'
# -- OR 2-DAYS AGO -- #
Search-Box 'CoolAwesomeFile' -UserID 123456789 -CreatedAfter (Get-Date).AddDays(-2)

# Search for items with the word "CoolAwesomeFile" in the default content fields (Name and Description).
# Search is limited to items with a file extension of ".txt" or ".doc".
Search-Box 'CoolAwesomeFile' -UserID 123456789 -FileExtensions 'txt', 'doc'
```

### Get User Information

```powershell
# Return the user specified by ID.
Get-BoxUser -UserID 123456789

# Search for a user with "dvader" in the name or login.
Get-BoxUser -SearchUser "dvader"

# Search for a user with "Darth Vader" in the name or login.
Get-BoxUser -SearchUser "Darth Vader"
```

## Common Properties to Review

Each of the code snippets below assume you have already acquired a list of Box items as seen in the previous examples.

### General Properties

Below is a listing of properties that are/can be available. The missing values can be added using the `Properties` parameter as seen in the examples above.

```powershell
$items[0] | fl *

FolderUploadEmail                     :
ItemCollection                        :
SyncState                             :
HasCollaborations                     :
Permissions                           : Box.V2.Models.BoxFolderPermission
AllowedInviteeRoles                   :
WatermarkInfo                         :
Metadata                              :
PurgedAt                              :
ContentCreatedAt                      :
ContentModifiedAt                     :
CanNonOwnersInvite                    :
AllowedSharedLinkAccessLevels         :
IsExternallyOwned                     :
ExpiresAt                             :
IsCollaborationRestrictedToEnterprise :
SequenceId                            :
ETag                                  : 1
Name                                  : Backup
Description                           :
Size                                  :
PathCollection                        : Box.V2.Models.BoxCollection`1[Box.V2.Models.BoxFolder]
CreatedAt                             : 10/26/2012 12:39:05 PM
ModifiedAt                            :
TrashedAt                             :
CreatedBy                             : Box.V2.Models.BoxUser
ModifiedBy                            :
OwnedBy                               : Box.V2.Models.BoxUser
Parent                                : Box.V2.Models.BoxFolder
ItemStatus                            :
SharedLink                            :
Tags                                  :
Id                                    : 453727509
Type                                  : folder
```

### Permissions

```powershell
$items[0].Permissions

CanInviteCollaborator : False
CanDownload           : True
CanUpload             : False
CanComment            : False
CanRename             : False
CanDelete             : False
CanShare              : True
CanSetShareAccess     : False
```

### Full Path

I may add a property later that does this automatically. For now these are just default BoxItem response properties.

```powershell
# Build out the full path for a single item in the array.
($items[0].PathCollection.Entries.Name + $items[0].Name) -join '/'

# Or do it for all items at once.
$items | ForEach-Object { ($_.PathCollection.Entries.Name + $_.Name) -join '/' }
```