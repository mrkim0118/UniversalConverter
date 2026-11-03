[Code]
var
  RuntimeDownloadPage: TDownloadWizardPage;

function HasWindowsDesktopRuntime8: Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.*'),
    FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure InitializeWizard;
begin
  RuntimeDownloadPage := CreateDownloadPage(
    'Installing .NET 8 Desktop Runtime',
    'Downloading the prerequisite from Microsoft...', nil);
  RuntimeDownloadPage.ShowBaseNameInsteadOfUrl := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID <> wpReady) or HasWindowsDesktopRuntime8 then
    Exit;

  RuntimeDownloadPage.Clear;
  RuntimeDownloadPage.Add(
    'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe',
    'windowsdesktop-runtime-8-x64.exe', '');
  RuntimeDownloadPage.Show;
  try
    try
      RuntimeDownloadPage.Download;
    except
      Log(GetExceptionMessage);
      if not RuntimeDownloadPage.AbortedByUser then
        SuppressibleMsgBox('Unable to download .NET 8 Desktop Runtime: ' +
          GetExceptionMessage, mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    RuntimeDownloadPage.Hide;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ExitCode: Integer;
begin
  Result := '';
  if HasWindowsDesktopRuntime8 then
    Exit;

  if not ShellExec('runas', ExpandConstant('{tmp}\windowsdesktop-runtime-8-x64.exe'),
    '/install /quiet /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ExitCode) then
  begin
    Result := 'Unable to start the .NET 8 Desktop Runtime installer.';
    Exit;
  end;

  if ExitCode = 3010 then
  begin
    NeedsRestart := True;
    Result := 'Restart Windows, then run this installer again to finish installing .NET 8 Desktop Runtime.';
  end
  else if (ExitCode <> 0) or not HasWindowsDesktopRuntime8 then
    Result := Format('.NET 8 Desktop Runtime installation failed (exit code %d).', [ExitCode]);
end;
