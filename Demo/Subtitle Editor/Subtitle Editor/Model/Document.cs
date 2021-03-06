﻿using AssLoader;
using GalaSoft.MvvmLight;
using SubtitleEditor.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using System.Collections.Concurrent;

namespace SubtitleEditor.Model
{
    class Document : ViewModelBase
    {
        public Document()
        {
            Application.Current.Suspending += onSuspending;
            reset();
        }

        private void updateTitle()
        {
            Title = SubtitleFile?.Name ?? Subtitle?.ScriptInfo.Title;
        }

        private string title;

        public string Title
        {
            get
            {
                return title;
            }
            private set
            {
                Set(nameof(Title),ref title, value);
            }
        }

        private LinkedList<IDocumentAction> actionList = new LinkedList<IDocumentAction>();
        private LinkedListNode<IDocumentAction> currentAction, savedAction;

        private void onSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            
        }

        public bool TryDo(IDocumentAction action)
        {
            if(subtitle == null)
                return false;
            try
            {
                action.Do(subtitle);
            }
            catch(Exception)
            {
                return false;
            }
            while(actionList.First.Next != currentAction)
                actionList.Remove(actionList.First.Next);
            actionList.AddAfter(actionList.First, action);
            currentAction = actionList.First.Next;
            modified();
            return true;
        }

        public void Do(IDocumentAction action)
        {
            if(subtitle == null)
                throw new InvalidOperationException($"{nameof(Subtitle)} is null");
            action.Do(subtitle);
            while(actionList.First.Next != currentAction)
                actionList.Remove(actionList.First.Next);
            actionList.AddAfter(actionList.First, action);
            currentAction = actionList.First.Next;
            modified();
        }

        public void Undo()
        {
            currentAction.Value.Undo(subtitle);
            currentAction = currentAction.Next;
            modified();
        }

        public void Redo()
        {
            currentAction = currentAction.Previous;
            currentAction.Value.Do(subtitle);
            modified();
        }

        private void modified()
        {
            RaisePropertyChanged(nameof(CanSave));
            RaisePropertyChanged(nameof(UndoAction));
            RaisePropertyChanged(nameof(RedoAction));
            RaisePropertyChanged(nameof(IsModified));
            updateTitle();
        }

        public IDocumentAction UndoAction => currentAction.Value;

        public IDocumentAction RedoAction => currentAction.Previous.Value;

        public bool IsModified => currentAction != savedAction;

        private Subtitle<ScriptInfo> subtitle;

        public Subtitle<ScriptInfo> Subtitle
        {
            get
            {
                return subtitle;
            }
            private set
            {
                Set(ref subtitle, value);
                if(value.ScriptInfo.SubtitleEditorConfig == null)
                    value.ScriptInfo.SubtitleEditorConfig = new ProjectConfig();
                RaisePropertyChanged(nameof(CanSave));
                updateTitle();
            }
        }

        private StorageFile subtitleFile;

        public StorageFile SubtitleFile
        {
            get
            {
                return subtitleFile;
            }
            private set
            {
                Set(ref subtitleFile, value);
                RaisePropertyChanged(nameof(CanSave));
                if(value != null)
                    StorageApplicationPermissions.MostRecentlyUsedList.Add(value, value.Name, RecentStorageItemVisibility.AppAndSystem);
                updateTitle();
            }
        }

        private void reset()
        {
            actionList.Clear();
            actionList.AddFirst((IDocumentAction)null);
            actionList.AddLast((IDocumentAction)null);
            currentAction = savedAction = actionList.Last;
        }

        public void NewSubtitle()
        {
            SubtitleFile = null;
            Subtitle = new Subtitle<ScriptInfo>(new ScriptInfo());
            currentAction = null;
            savedAction = null;
            reset();
            modified();
        }

        public async Task OpenFileAsync(StorageFile file)
        {
            if(file == null)
                throw new ArgumentNullException(nameof(file));
            SubtitleFile = file;
            using(var stream = await SubtitleFile.OpenStreamForReadAsync())
            using(var reader = new StreamReader(stream))
                Subtitle = AssLoader.Subtitle.Parse<ScriptInfo>(reader);
            reset();
            modified();
        }

        public bool CanSave => subtitle != null && subtitleFile != null && IsModified;

        public async Task SaveAsync()
        {
            if(!CanSave)
                throw new InvalidOperationException("Can't save now.");
            try
            {
                using(var stream = await SubtitleFile.OpenStreamForWriteAsync())
                {
                    stream.SetLength(0);
                    using(var writer = new StreamWriter(stream))
                        subtitle.Serialize(writer);
                }
            }
            catch(Exception)
            {
                SubtitleFile = null;
                throw;
            }
            savedAction = currentAction;
            RaisePropertyChanged(nameof(IsModified));
        }

        public async Task SaveFileAsync(StorageFile file)
        {
            if(file == null)
                throw new ArgumentNullException(nameof(file));
            SubtitleFile = file;
            await SaveAsync();
        }

        public override void Cleanup()
        {
            Application.Current.Suspending -= onSuspending;
            reset();
            subtitle = null;
            subtitleFile = null;
            base.Cleanup();
        }
    }
}
