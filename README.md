# TimetableApp
Timetable management and automatic online class joining. Created for the Uno Platform, which works on all your devices.

## Try it online:
![WASM screenshot](Images/WASM.png)

## Screenshots:
![UWP screenshot](Images/UWP.png)  
![GTK screenshot](Images/Gtk.png)
![Android screenshot](Images/Android.png)

## Data format: 
The application downloads the timetable from a URL, provided by the organization. The URL must lead to a JSON file, with this format:

```
{  
   "MD5":"MD5 of real timetable file, for validation.",  
   "Location":"https://example.com/url-to-real-timetable-file"  
}
```

The real timetable file MUST be in this format:

```
{
   "Name":"Name of timetable",  
   "UpdateURL":"Where to check for the next version",  
   "Lessons":[  
        [],  
        [  
               {
                   "StartTime": "07:30:00",  
                   "EndTime": "08:10:00",  
                   "Credentials": {  
                       "$type": "TimetableApp.Core.Zoom.ZoomCredentials, $ASSEMBLY_NAME",  
                       "ID": "room-id",  
                       "Password": "password"  
                       },  
                   "Subject": "subject name",  
                   "TeacherName": "your teacher here",  
                   "Notes": "",  
                   "AdditionalTags": {}  
                   },  
        ]  
   ]  
}  
```

### Remarks:
- `StartTime` and `EndTime`
- `$type` for Credentials is the credentials class you want to use. Currently, we only support Zoom credentials.
- `$ASSEMBLY_NAME` will be internally replaced with the application's assembly name. This is used for compatibility with old versions of the app (Timetable.NET), and with the generator.
- `AdditionalTags` is a `Dictionary<string, string>`, which contains custom properties. 
git 
## Upcoming plans:  
- Timetable Editor: We will create this if the project gets more popularity, as not many teachers know JSON!
- Custom credentials: Currently, our users only use Zoom for online classes. Extensions might be done on user request.

## Related issues:  
### Uno Platform's macOS specific issues:  
- [#7319](https://github.com/unoplatform/uno/issues/7319) \[macOS\] Application freezes when DataGrid contains data. (Not fixed but closed by the Uno Platform maintainers).
- [#8110](https://github.com/unoplatform/uno/issues/8110) \[macOS\] DataGrid on a TabView not appearing in macOS (Similar issue, TimetableApp also uses a DataGrid on a TabView).

This issue is making _any_ support for macOS impossible.

### Uno Platform's Linux specific issues:
- [#7212](https://github.com/unoplatform/uno/issues/7212) \[Skia.GTK\] Applications do not respond to Dark mode themes. The method Uno Platform uses to detect Dark/Light mode for Skia.GTK is currently faulty.
- [#8643](https://github.com/unoplatform/uno/issues/8643) \[Skia\] OpenGL render surface fails with a null reference on WSL. TimetableApp is still forced to use software rendering.
- [#8661](https://github.com/unoplatform/uno/issues/8661) MessageDialog does not show Close button on Skia.GTK (Linux, WSL). Currently, TimetableApp.Skia.GTK still uses the [native MessageDialog hack](https://github.com/AzureAms/TimetableApp.Uno/blob/master/TimetableApp/TimetableApp.Skia.Gtk/MessageDialog.cs).