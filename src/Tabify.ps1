function FormatItems($projectItems) {
    $projectItems |
    % {
        # Write-Host "    Examining item: $($_.Name)";

        if ($_.Name -and $_.Name.ToLower().EndsWith(".cs") `
            -and (-not $_.Name.ToLower().Contains(".designer."))) {

            $win = $_.Open('{7651A701-06E5-11D1-8EBD-00A0C90F26EA}');
            $win.Activate();

            $dte.ExecuteCommand('Edit.SelectAll');
            $dte.ExecuteCommand('Edit.TabifySelectedLines');

            if (!$_.Saved) {
                Write-Host "    Saving modified file: $($_.Name)";
                $dte.ExecuteCommand('File.SaveSelectedItems');
            }

            $dte.ExecuteCommand('Window.CloseDocumentWindow');
        }

        if ($_.ProjectItems -and ($_.ProjectItems.Count -gt 0)) {
            # Write-Host "    Opening sub-items of $($_.Name)";

            FormatItems($_.ProjectItems);
        }
    };
}

$dte.Solution.Projects | % {
    Write-Host "-- Project: $($_.Name)";

    FormatItems($_.ProjectItems)
};