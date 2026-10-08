using System;
using System.IO;
using ProfessorSprat.Core;
using UnityEngine;

namespace ProfessorSprat.Gameplay.Flow
{
    /// <summary>
    /// TDD §B-S SaveService (§11.4 zone-completion checkpoints): loads <c>save.json</c> once per session, records each
    /// <see cref="ZoneClearedEvent"/> and writes atomically (temp file + replace). A corrupt file is renamed
    /// <c>save.bad.json</c> and the session starts empty. Mid-zone progress is never written.
    /// </summary>
    public sealed class SaveService : MonoBehaviour
    {
        #region Fields

        [Tooltip("Optional override (tests); empty = Application.persistentDataPath/save.json.")]
        [SerializeField] private string _fileOverride;

        #endregion

        #region Public Methods

        public SessionModel Session { get; private set; } = new SessionModel();

        public string FilePath => string.IsNullOrEmpty(_fileOverride) ? Path.Combine(Application.persistentDataPath, "save.json") : _fileOverride;

        public bool HasSave => File.Exists(FilePath);

        public SessionModel Load()
        {
            Session = new SessionModel();
            if (!HasSave)
            {
                return Session;
            }

            try
            {
                Session = SaveSchema.Migrate(JsonUtility.FromJson<SessionModel>(File.ReadAllText(FilePath)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{nameof(SaveService)}: unreadable save ({exception.Message}); kept as save.bad.json, starting empty.", this);
                File.Copy(FilePath, Path.ChangeExtension(FilePath, ".bad.json"), true);
                Session = new SessionModel();
            }

            return Session;
        }

        public void Save()
        {
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(Session, true));
            if (File.Exists(FilePath))
            {
                File.Replace(temp, FilePath, null);
            }
            else
            {
                File.Move(temp, FilePath);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            EventBus.Subscribe<ZoneClearedEvent>(OnZoneCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ZoneClearedEvent>(OnZoneCleared);
        }

        private void OnApplicationQuit()
        {
            if (Session.completedZones.Count > 0)
            {
                Save();
            }
        }

        #endregion

        #region Private Methods

        private void OnZoneCleared(ZoneClearedEvent cleared)
        {
            Session.Record(cleared.LevelId, cleared.ZoneId, cleared.FliesCollected, cleared.FliesTotal, cleared.ElapsedSeconds);
            Save();
        }

        #endregion
    }
}
