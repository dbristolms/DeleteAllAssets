# DeleteAllAssets
**This application deletes all assets within a Media Services account and the Storage containers that back those assets.**

This application was written in C#.NET.  It uses the Azure Media Services [v3 API](https://docs.microsoft.com/en-us/azure/media-services/latest/media-services-apis-overview).

If you do not want to delete your Media Services account, but want to get rid of all of the content within the account you can delete all of the assets.  With Media Services, when you delete an asset Media Services will delete the Azure Storage container that backs the Media Services asset.  This application will loop through all of the assets within your Media Services account and delete them.

The application consists of two primary loops.  The first loop obtains all of the asset names using the [list](https://docs.microsoft.com/en-us/rest/api/media/assets/list) API for assets.  Since the list API returns a max of 1000 assets it will page through that list.  Once we have all of the asset names the application will prompt you to delete the first asset.  You can choose to delete assets individually or all of them.
