# DDC Improver
![Screenshot of version 3.0](https://i.imgur.com/Rz6sHLp.png)

A tool for processing Rocksmith 2014 XML files.
It originated as a command line program I wrote in Ruby to automate a workaround to prevent DDC (Dynamic Difficulty Creator) from moving sections that are not placed on the first beat of a measure.
Since than I've added more features and eventually decided to make it into a GUI program using C#, largely as a learning experience.
 
It is called DDC Improver since I couldn't think of a better name.
Some of the features in the program have nothing to do with DDC and it can also process files with manual DD.
 
# Main Features

- Can process multiple files simultaneously
- Prevents DDC from moving sections that are not on the first beat of a measure
- Restores FHPs set at the beginning of noguitar sections
- Adjusts the lengths of handshapes if they are too close together
- Fixes phrases that have only one level by adding a second level to them
- Can automatically place crowd events
- Move phrases/sections off beat with special phrase names
- Removes beats that come after the audio has ended
- Checks the XML for issues
- Can quickly remove DD from files

# Libraries Used

- [.NET Core](https://github.com/dotnet/core)
- [WPF](https://github.com/dotnet/wpf)
- [Avalonia](https://github.com/AvaloniaUI/Avalonia)
- [ReactiveUI](https://github.com/reactiveui/ReactiveUI)
- [DynamicData](https://github.com/reactiveui/DynamicData)
- [Fody](https://github.com/Fody/Fody)
- [xunit](https://github.com/xunit/xunit)
- [Moq](https://github.com/moq/moq4)
- [FluentAssertions](https://github.com/fluentassertions/fluentassertions)
