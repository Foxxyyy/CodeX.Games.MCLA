using CodeX.Core.Engine;
using CodeX.Core.Utilities;
using CodeX.Forms.Explorer;
using CodeX.Forms.Themes;
using CodeX.Games.MCLA.RPF3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CodeX.Games.MCLA.Files
{
    //Playback window for an animation pack: pick a model, pick an animation, watch it move.
    //
    //It drives a PreviewForm through that form's public surface (its Level, its entities and
    //LoadPiecePack) and runs the clock itself, so nothing in CodeX.Core or CodeX.Forms had to
    //change to make this work.
    public class MclaAnimationForm : Form
    {
        private readonly PreviewForm _preview;
        private readonly Rpf3FileManager _fman;
        private XapkFile _pack;

        private readonly ComboBox _packBox = new();
        private readonly TextBox _packFilter = new();
        private readonly ComboBox _modelBox = new();
        private readonly TextBox _modelFilter = new();
        private readonly ListBox _animList = new();
        private readonly TextBox _animFilter = new();
        private readonly Button _playButton = new();
        private readonly Button _rewindButton = new();
        private readonly CheckBox _loopBox = new();
        private readonly CheckBox _rootMotionBox = new();
        private readonly TrackBar _scrub = new();
        private readonly Label _status = new();
        private readonly System.Windows.Forms.Timer _timer = new();
        private readonly System.Diagnostics.Stopwatch _clock = new();

        private List<GameArchiveFileInfo> _packs = [];
        private List<GameArchiveFileInfo> _models = [];
        private XapkAnimation[] _animations = [];
        private MclaAnimator _animator;
        private bool _scrubbing;
        private bool _loadingPack;
        private string _loadedModel;

        //Walking the whole entry dictionary takes a moment on an archive this size and the answer
        //never changes, so it is done once per file manager.
        private static readonly Dictionary<FileManager, (List<GameArchiveFileInfo> packs, List<GameArchiveFileInfo> models)> Catalogue = [];

        public MclaAnimationForm(PreviewForm preview, Rpf3FileManager fman)
        {
            _preview = preview;
            _fman = fman;

            Text = "Animation - CodeX";
            ClientSize = new Size(420, 700);
            StartPosition = FormStartPosition.Manual;
            MinimizeBox = false;

            var y = 8;
            Controls.Add(Caption("Animation pack", 8, y));
            y += 18;
            Place(_packFilter, 8, y, 404, 22);
            _packFilter.PlaceholderText = "filter packs...";
            _packFilter.TextChanged += (s, e) => RefreshPacks();
            y += 26;
            Place(_packBox, 8, y, 404, 22);
            _packBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _packBox.SelectedIndexChanged += PackBox_SelectedIndexChanged;
            y += 30;

            Controls.Add(Caption("Model", 8, y));
            y += 18;
            Place(_modelFilter, 8, y, 404, 22);
            _modelFilter.PlaceholderText = "filter models...";
            _modelFilter.TextChanged += (s, e) => RefreshModels();
            y += 26;
            Place(_modelBox, 8, y, 404, 22);
            _modelBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modelBox.SelectedIndexChanged += ModelBox_SelectedIndexChanged;
            y += 30;

            Controls.Add(Caption("Animations", 8, y));
            y += 18;
            Place(_animFilter, 8, y, 404, 22);
            _animFilter.PlaceholderText = "filter animations...";
            _animFilter.TextChanged += (s, e) => RefreshAnimations();
            y += 26;
            Place(_animList, 8, y, 404, 380);
            _animList.SelectedIndexChanged += AnimList_SelectedIndexChanged;
            y += 388;

            Place(_scrub, 4, y, 412, 30);
            _scrub.Minimum = 0;
            _scrub.Maximum = 1000;
            _scrub.TickStyle = TickStyle.None;
            _scrub.Scroll += Scrub_Scroll;
            _scrub.MouseDown += (s, e) => _scrubbing = true;
            _scrub.MouseUp += (s, e) => _scrubbing = false;
            y += 34;

            Place(_playButton, 8, y, 80, 24);
            _playButton.Text = "Play";
            _playButton.Click += PlayButton_Click;

            Place(_rewindButton, 94, y, 80, 24);
            _rewindButton.Text = "Rewind";
            _rewindButton.Click += RewindButton_Click;

            Place(_loopBox, 184, y + 4, 60, 20);
            _loopBox.Text = "Loop";
            _loopBox.Checked = true;

            Place(_rootMotionBox, 248, y + 4, 100, 20);
            _rootMotionBox.Text = "Root motion";
            _rootMotionBox.CheckedChanged += (s, e) => { if (_animator != null) _animator.EnableRootMotion = _rootMotionBox.Checked; };
            y += 30;

            Place(_status, 8, y, 404, 20);
            _status.Text = "no animation";

            _timer.Interval = 33;
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Theme.Apply(this);
        }

        private void Place(Control c, int x, int y, int w, int h)
        {
            c.SetBounds(x, y, w, h);
            Controls.Add(c);
        }

        private static Label Caption(string text, int x, int y)
        {
            var l = new Label();
            l.SetBounds(x, y, 200, 16);
            l.Text = text;
            return l;
        }

        public void LoadPack(XapkFile pack)
        {
            _pack = pack;
            Text = (pack?.FileInfo?.Name ?? "Animation") + " - CodeX";
            if (_packs.Count == 0) RefreshPacks();
            var name = pack?.FileInfo?.Name;
            if (name != null)
            {
                var i = _packs.FindIndex(p => p.Name == name);
                if (i >= 0 && _packBox.SelectedIndex != i)
                {
                    _loadingPack = true;
                    _packBox.SelectedIndex = i;
                    _loadingPack = false;
                }
            }
            RefreshModels();
            RefreshAnimations();
        }

        private (List<GameArchiveFileInfo> packs, List<GameArchiveFileInfo> models) Catalog()
        {
            if (_fman?.EntryDict == null) return ([], []);
            lock (Catalogue)
            {
                if (Catalogue.TryGetValue(_fman, out var known)) return known;
            }
            var packs = new List<GameArchiveFileInfo>();
            var models = new List<GameArchiveFileInfo>();
            foreach (var kvp in _fman.EntryDict)
            {
                if (kvp.Value is not GameArchiveFileInfo fi) continue;
                if (fi.Name.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase)) packs.Add(fi);
                else if (fi.Name.EndsWith(".xrsc", StringComparison.OrdinalIgnoreCase)) models.Add(fi);
            }
            packs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            models.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            var entry = (packs, models);
            lock (Catalogue)
            {
                Catalogue[_fman] = entry;
            }
            return entry;
        }

        private void RefreshPacks()
        {
            var filter = _packFilter.Text?.Trim() ?? string.Empty;
            _packs = [];
            foreach (var fi in Catalog().packs)
            {
                if (filter.Length > 0 && fi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _packs.Add(fi);
            }
            _packBox.BeginUpdate();
            _packBox.Items.Clear();
            foreach (var p in _packs) _packBox.Items.Add(p.Name);
            _packBox.EndUpdate();
        }

        private void RefreshModels()
        {
            var filter = _modelFilter.Text?.Trim() ?? string.Empty;
            _models = [];
            foreach (var fi in Catalog().models)
            {
                if (filter.Length > 0 && fi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _models.Add(fi);
                if (_models.Count >= 2000) break;
            }
            _modelBox.BeginUpdate();
            _modelBox.Items.Clear();
            foreach (var m in _models) _modelBox.Items.Add(m.Name);
            _modelBox.EndUpdate();
        }

        private void RefreshAnimations()
        {
            var filter = _animFilter.Text?.Trim() ?? string.Empty;
            var all = _pack?.Anims ?? [];
            _animations = filter.Length > 0
                ? all.Where(a => (a.Name ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray()
                : all;

            _animList.BeginUpdate();
            _animList.Items.Clear();
            foreach (var a in _animations)
            {
                //"face only" is the tell for the _IM variants, which animate lips and eyebrows and
                //leave the body where it is - they look like broken playback otherwise.
                var moving = a.MovingBodyTrackCount > 0 ? $", {a.MovingBodyTrackCount} moving" : ", face only";
                _animList.Items.Add($"{a.Name}   [{a.Duration:0.##}s{moving}]");
            }
            _animList.EndUpdate();
        }

        private void PackBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingPack) return;
            var i = _packBox.SelectedIndex;
            if (i < 0 || i >= _packs.Count) return;
            try
            {
                var pack = _fman.LoadPiecePack(_packs[i]) as XapkFile;
                if (pack == null) { _status.Text = "could not read " + _packs[i].Name; return; }
                _pack = pack;
                Text = _packs[i].Name + " - CodeX";
                SetAnimator(null);
                RefreshAnimations();
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
            }
        }

        //A cutscene animation is named after the actor it drives, e.g.
        //"story/cut_x/Entity/bookeGF_drv04_name_drv_fc_003_set/...", so the model can often be
        //found without the user hunting for it.
        private GameArchiveFileInfo GuessModel(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return null;
            var parts = animName.Split(new[] { '/', (char)92 }, StringSplitOptions.RemoveEmptyEntries);
            var candidates = new List<string>();
            foreach (var part in parts)
            {
                var idx = part.IndexOf("_name_", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) candidates.Add(part[(idx + 6)..]);
                candidates.Add(part);
            }
            candidates.Reverse();

            foreach (var c in candidates)
            {
                if (c.Length < 3) continue;
                foreach (var fi in Catalog().models)
                {
                    if (!fi.Name.StartsWith(c, StringComparison.OrdinalIgnoreCase)) continue;
                    if (fi.Name.Contains("lod", StringComparison.OrdinalIgnoreCase)) continue;
                    return fi;
                }
            }
            return null;
        }

        private bool LoadModel(GameArchiveFileInfo fi)
        {
            try
            {
                var pcp = _fman.LoadPiecePack(fi, null, true);
                if (pcp == null) { _status.Text = "could not load " + fi.Name; return false; }
                SetAnimator(null);
                _preview.LoadPiecePack(pcp, _fman);
                _loadedModel = fi.Name;
                return true;
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                return false;
            }
        }

        private void ModelBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var i = _modelBox.SelectedIndex;
            if (i < 0 || i >= _models.Count) return;
            if (LoadModel(_models[i])) AnimList_SelectedIndexChanged(null, null);
        }

        private void AnimList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var i = _animList.SelectedIndex;
            if (i < 0 || i >= _animations.Length) return;
            var anim = _animations[i];

            if (_loadedModel == null)
            {
                var guess = GuessModel(anim.Name);
                if (guess != null) LoadModel(guess);
            }

            var animator = new MclaAnimator(anim)
            {
                Loop = _loopBox.Checked,
                EnableRootMotion = _rootMotionBox.Checked,
            };
            if (!SetAnimator(animator))
            {
                _status.Text = "pick a model with a skeleton first";
                return;
            }

            _scrub.Value = 0;
            _animator.Playing = true;
            _playButton.Text = "Pause";
            _clock.Restart();
            _animator.ApplyTime(0.0f);
            _status.Text = $"{anim.Duration:0.##}s{Coverage()}";
            Log($"selected '{anim.Name}' duration={anim.Duration:0.###}{Coverage()}");
        }

        //Bind the animator to every entity in the preview that carries a skeleton. Only the first
        //one holds it, with the rest in Targets, so the clock cannot be advanced more than once.
        private bool SetAnimator(MclaAnimator animator)
        {
            var ents = new List<Entity>();
            foreach (var ent in _preview?.Level?.Entities ?? [])
            {
                if ((ent.Skeleton ?? ent.Piece?.Skeleton)?.Bones != null) ents.Add(ent);
            }

            _animator?.Release();
            foreach (var e in ents)
            {
                _preview.Level.AnimEntities?.Remove(e);
                e.Animator = null;
            }
            _animator = animator;
            if (animator == null) return true;
            if (ents.Count == 0) return false;

            foreach (var e in ents)
            {
                var skel = e.Skeleton ?? e.Piece?.Skeleton;
                if (skel == null) continue;
                e.SetSkeleton(skel);
                skel.AnimateRenderables = true;
            }
            animator.Target = ents[0];
            animator.Targets = [.. ents];
            return true;
        }

        private string Coverage()
        {
            if (_animator == null || _animator.TrackCount == 0) return string.Empty;
            var anim = CurrentAnimation();
            var extra = anim != null && anim.MovingBodyTrackCount == 0
                ? "   -   face only, the body does not move" : string.Empty;
            return $"   -   {_animator.MatchedTrackCount} of {_animator.TrackCount} tracks fit this model{extra}";
        }

        private XapkAnimation CurrentAnimation()
        {
            var i = _animList.SelectedIndex;
            return (i >= 0 && i < _animations.Length) ? _animations[i] : null;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (_animator == null) return;
            _animator.Playing = !_animator.Playing;
            _playButton.Text = _animator.Playing ? "Pause" : "Play";
            _clock.Restart();
        }

        private void RewindButton_Click(object sender, EventArgs e)
        {
            if (_animator == null) return;
            _animator.ApplyTime(0.0f);
            _scrub.Value = 0;
            _clock.Restart();
        }

        private void Scrub_Scroll(object sender, EventArgs e)
        {
            var anim = CurrentAnimation();
            if (_animator == null || anim == null) return;
            _animator.Playing = false;
            _playButton.Text = "Play";
            _animator.ApplyTime(anim.Duration * _scrub.Value / 1000.0f);
        }

        //The window runs the clock and poses the model itself, so playback does not depend on the
        //preview's level being ticked.
        private void Timer_Tick(object sender, EventArgs e)
        {
            var anim = CurrentAnimation();
            if (_animator == null || anim == null || anim.Duration <= 0) { _clock.Reset(); return; }
            if (!_animator.Playing || _scrubbing) { _clock.Reset(); return; }

            if (!_clock.IsRunning) _clock.Restart();
            var step = _clock.Elapsed.TotalSeconds;
            _clock.Restart();

            var t = _animator.CurrentTime + step;
            if (t > anim.Duration)
            {
                if (_loopBox.Checked) t %= anim.Duration;
                else
                {
                    t = anim.Duration;
                    _animator.Playing = false;
                    _playButton.Text = "Play";
                }
            }
            _animator.ApplyTime((float)t);
            _scrub.Value = Math.Clamp((int)(t / anim.Duration * 1000.0), 0, 1000);
            _status.Text = $"{t:0.00} / {anim.Duration:0.00}s{Coverage()}";
            if (++_ticks % 30 == 0) Log($"tick {_ticks}: t={t:0.###}s step={step:0.####} {Describe()}");
        }

        private int _ticks;

        //A trace of what the window actually did, for diagnosing playback without watching the
        //screen. Written next to the executable as animation.log.
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "animation.log");
        private static void Log(string line)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff}  {line}{Environment.NewLine}");
            }
            catch { }
        }

        private string Describe()
        {
            var ent = _animator?.Target;
            var skel = ent?.Skeleton ?? ent?.Piece?.Skeleton;
            if (skel?.Bones == null) return "no skeleton";
            var head = skel.BonesDictionary != null && skel.BonesDictionary.TryGetValue("head", out var h)
                ? h : skel.Bones[skel.Bones.Length / 2];
            var p = head.AnimTransform.Translation;
            return $"animateRenderables={skel.AnimateRenderables} {head.Name}=({p.X:0.###},{p.Y:0.###},{p.Z:0.###})";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            SetAnimator(null);
            base.OnFormClosed(e);
        }
    }
}
