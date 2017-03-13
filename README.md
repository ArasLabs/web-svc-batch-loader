>## Archived Aras Community Project
*This project has been migrated to GitHub from the old Aras Projects page (http://www.aras.com/projects). As an Archived project, this project is no longer being actively developed or maintained.*

>*For current projects, please visit the new Aras Community Projects page on the updated Aras Community site: http://community.aras.com/projects*

# Windows Service Based Batch Loader

Example Visual Studio C# project to create a complete Windows Service that runs periodically, watching a folder for data, to be batchloaded into an Innovator instance.

When there is data, the program connects to Innovator as a web service, and uploads the data.

All messages are sent to the standard Windows Event Viewer. The run interval and the logging levels are configurable.

This is a good example project (fully functional!) of 2 things (1) how to write a windows service and (2) connecting to Innovator as a web service for automatically uploading data.

In the example, the windows service is named WebLogLoader Service. It watches the c:\InetPub\Logs folder from our web site, and loads cookie and page visit information into Innovator, loading an ItemType and a RelationshipType.

## History

Release notes/descriptions for the original project posted on the previous Aras Projects page.

Release | Notes
--------|--------
[v1.0](https://github.com/ArasLabs/cmii-wizard-w-bulk-change/releases/tag/v1.0) | Initial version

#### Supported Aras Versions

Project | Aras
--------|------
[v1.0](https://github.com/ArasLabs/cmii-wizard-w-bulk-change/releases/tag/v1.0) | Aras 9.3

> ###### *Please note: Aras Community Projects are provided on an "as-is" basis.*

>*Due to the wide array of possible business requirements, customizations, and use cases, we cannot guarantee compatibility or support for any given project.*

>*If you experience issues with this or any other Aras Community Project, please contact the project author and file an issue on the project's GitHub repository. You can also check out the [Aras Developer Forums](http://community.aras.com/forums/) to see if any other community members have experienced the same issue.*

## Credits

**Project Owner:** Peter Schroer, Aras Corporation

**Created On:** December 28, 2007

## License

This project is published under the Microsoft Public License license (MS-PL). See the [LICENSE file](./LICENSE.md) for license rights and limitations.
