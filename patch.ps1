$path = 'e:\LiveWallpaperApp\Views\MainWindow.xaml'
$content = [System.IO.File]::ReadAllText($path)

$dashStart = '<Grid Visibility="{Binding SelectedPage, Converter={StaticResource PageVisibilityConverter}, ConverterParameter=Dashboard}">'
$dashEnd = '</Grid>'

$libStart = '<Grid Visibility="{Binding SelectedPage, Converter={StaticResource PageVisibilityConverter}, ConverterParameter=Library}">'
$libEnd = '</Grid>'

# Find Dash
$idxDashStart = $content.IndexOf($dashStart)
$idxDashEnd = $content.IndexOf($dashEnd, $idxDashStart)
while ($content.Substring($idxDashStart, $idxDashEnd - $idxDashStart).Split('<Grid').Length - 1 -ne $content.Substring($idxDashStart, $idxDashEnd - $idxDashStart).Split('</Grid').Length - 1 + 1) {
    $idxDashEnd = $content.IndexOf($dashEnd, $idxDashEnd + 1)
}
$idxDashEnd += $dashEnd.Length

# Find Lib
$idxLibStart = $content.IndexOf($libStart)
$idxLibEnd = $content.IndexOf($libEnd, $idxLibStart)
while ($content.Substring($idxLibStart, $idxLibEnd - $idxLibStart).Split('<Grid').Length - 1 -ne $content.Substring($idxLibStart, $idxLibEnd - $idxLibStart).Split('</Grid').Length - 1 + 1) {
    $idxLibEnd = $content.IndexOf($libEnd, $idxLibEnd + 1)
}
$idxLibEnd += $libEnd.Length

$newDashboard = @"
<Grid Visibility="{Binding SelectedPage, Converter={StaticResource PageVisibilityConverter}, ConverterParameter=Dashboard}">
    <Grid.RowDefinitions>
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="18" />
        <ColumnDefinition Width="340" />
    </Grid.ColumnDefinitions>

    <!-- LEFT SIDE: LIBRARY GRID -->
    <Border Grid.Column="0" Style="{StaticResource PanelStyle}" Padding="0" ClipToBounds="True">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            
            <Grid Margin="18,18,18,14">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="160" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" />
                <ComboBox Grid.Column="2" ItemsSource="{Binding SortModes}" SelectedItem="{Binding SelectedSortMode}" />
                <Button Grid.Column="4" Content="Refresh" Style="{StaticResource SecondaryButtonStyle}" MinWidth="90" Command="{Binding RefreshLibraryCommand}" />
            </Grid>

            <ListBox Grid.Row="1"
                     ItemsSource="{Binding FilteredLibraryPreviews}"
                     ItemContainerStyle="{StaticResource CardItemStyle}"
                     Background="Transparent"
                     BorderThickness="0"
                     Padding="18,0,0,0"
                     ScrollViewer.HorizontalScrollBarVisibility="Disabled"
                     VirtualizingPanel.IsVirtualizing="True"
                     VirtualizingPanel.VirtualizationMode="Recycling">
                <ListBox.ItemsPanel>
                    <ItemsPanelTemplate>
                        <VirtualizingStackPanel />
                    </ItemsPanelTemplate>
                </ListBox.ItemsPanel>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Width="260"
                                Height="192"
                                CornerRadius="14"
                                Background="{DynamicResource SoftPanelBrush}"
                                BorderBrush="{DynamicResource BorderBrush}"
                                BorderThickness="1">
                            <Border.Style>
                                <Style TargetType="Border">
                                    <Setter Property="RenderTransformOrigin" Value="0.5,0.5" />
                                    <Setter Property="RenderTransform">
                                        <Setter.Value>
                                            <ScaleTransform ScaleX="1" ScaleY="1" />
                                        </Setter.Value>
                                    </Setter>
                                    <Style.Triggers>
                                        <Trigger Property="IsMouseOver" Value="True">
                                            <Setter Property="BorderBrush" Value="{DynamicResource BorderGlowBrush}" />
                                            <Setter Property="RenderTransform">
                                                <Setter.Value>
                                                    <ScaleTransform ScaleX="1.015" ScaleY="1.015" />
                                                </Setter.Value>
                                            </Setter>
                                        </Trigger>
                                    </Style.Triggers>
                                </Style>
                            </Border.Style>
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="128" />
                                    <RowDefinition Height="*" />
                                </Grid.RowDefinitions>
                                <controls:AnimatedThumbnailControl VideoPath="{Binding PreviewPath}"
                                                                   PlayerOptions="{Binding DataContext.ThumbnailVlcOptions, RelativeSource={RelativeSource AncestorType=ListBox}}" />
                                <Border Grid.Row="0"
                                        HorizontalAlignment="Left"
                                        VerticalAlignment="Top"
                                        Margin="10"
                                        Padding="9,4"
                                        CornerRadius="10"
                                        Background="{DynamicResource AccentBrush}"
                                        Visibility="{Binding IsActiveWallpaper, Converter={StaticResource BooleanToVisibilityConverter}}">
                                    <TextBlock Text="ACTIVE"
                                               Foreground="{DynamicResource AccentTextBrush}"
                                               FontWeight="SemiBold"
                                               FontSize="10" />
                                </Border>
                                <Grid Grid.Row="1" Margin="14,10,14,12">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*" />
                                        <ColumnDefinition Width="Auto" />
                                    </Grid.ColumnDefinitions>
                                    <StackPanel>
                                        <TextBlock Text="{Binding DisplayName}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis" />
                                        <TextBlock Text="{Binding Author}" Foreground="{DynamicResource MutedTextBrush}" FontSize="11" TextTrimming="CharacterEllipsis" />
                                    </StackPanel>
                                    <Button Grid.Column="1"
                                            Content="Use"
                                            Style="{StaticResource SecondaryButtonStyle}"
                                            MinWidth="60"
                                            Command="{Binding DataContext.SelectLibraryItemCommand, RelativeSource={RelativeSource AncestorType=ListBox}}"
                                            CommandParameter="{Binding}" />
                                </Grid>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Grid>
    </Border>

    <!-- RIGHT SIDE: PROPERTIES & PREVIEW -->
    <StackPanel Grid.Column="2">
        <Border Style="{StaticResource PanelStyle}" Margin="0,0,0,16" Padding="0" ClipToBounds="True">
            <StackPanel>
                <Border Height="180">
                    <controls:LiveWallpaperPreviewControl VideoPath="{Binding VideoPath}"
                                                          PlayerOptions="{Binding ThumbnailVlcOptions}"
                                                          IsPreviewActive="{Binding IsDashboardActive}" />
                </Border>
                <StackPanel Padding="18">
                    <TextBlock Text="Selected Wallpaper" Style="{StaticResource TitleTextStyle}" />
                    <TextBlock Text="Preview and properties" Style="{StaticResource CaptionStyle}" />
                    
                    <Grid Margin="0,10,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="10" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>
                        <TextBox Text="{Binding VideoPath, UpdateSourceTrigger=PropertyChanged}" />
                        <Button Grid.Column="2" Content="Browse" Style="{StaticResource SecondaryButtonStyle}" MinWidth="80" Command="{Binding BrowseCommand}" />
                    </Grid>
                    
                    <TextBlock Text="Display Monitor" Style="{StaticResource CaptionStyle}" Margin="0,16,0,8" />
                    <ComboBox ItemsSource="{Binding MonitorSelections}"
                              SelectedValuePath="DeviceName"
                              SelectedValue="{Binding SelectedMonitorDeviceName}" />
                    
                    <Grid Margin="0,20,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="10" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <Button Grid.Column="0" Content="Apply Wallpaper" Style="{StaticResource PrimaryButtonStyle}" MinWidth="110" Command="{Binding ApplyCommand}" />
                        <Button Grid.Column="2" Content="Stop" Style="{StaticResource SecondaryButtonStyle}" MinWidth="110" Command="{Binding StopCommand}" />
                    </Grid>
                </StackPanel>
            </StackPanel>
        </Border>

        <Border Style="{StaticResource PanelStyle}">
            <StackPanel>
                <TextBlock Text="Quick Performance" Style="{StaticResource TitleTextStyle}" />
                <TextBlock Text="Limit resources immediately." Style="{StaticResource CaptionStyle}" />
                <ComboBox ItemsSource="{Binding PerformanceModes}" SelectedItem="{Binding SelectedPerformanceMode}" />
            </StackPanel>
        </Border>
    </StackPanel>
</Grid>
"@

$newLibrary = '<Grid Visibility="Collapsed"></Grid>'

if ($idxLibStart -gt $idxDashStart) {
    $content = $content.Remove($idxLibStart, $idxLibEnd - $idxLibStart).Insert($idxLibStart, $newLibrary)
    $content = $content.Remove($idxDashStart, $idxDashEnd - $idxDashStart).Insert($idxDashStart, $newDashboard)
} else {
    $content = $content.Remove($idxDashStart, $idxDashEnd - $idxDashStart).Insert($idxDashStart, $newDashboard)
    $content = $content.Remove($idxLibStart, $idxLibEnd - $idxLibStart).Insert($idxLibStart, $newLibrary)
}

[System.IO.File]::WriteAllText($path, $content)
