using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Assets {
	public class AnimationAsset : DisposableAsset<Bitmap> {
		private struct AnimationData {
			public long FrameDuration;
			public int FrameCount;
			public AnimationData(long duration, int count) {
				FrameDuration = duration;
				FrameCount = count;
			}
		}
		private AnimationData[] _animations;
		private float _frameWidth;
		private float _frameHeight;
		private int _animationCount;
		private int _currentAnim;
		public int CurrentAnimation {
			get { return _currentAnim; }
			set {
				if (value >= _animationCount)
					_currentAnim = _animationCount - 1;
				else if (value < 0)
					_currentAnim = 0;
				else
					_currentAnim = value;
			}
		}

		private long _frameAcc;
		private int _currentFrame;

		public AnimationAsset(Bitmap bitmap, string file)
			: base(bitmap) {
				LoadAnimation(file);
		}

		public void LoadAnimation(string filePath) {
			LoadAnimation(new FileInfo(filePath));
		}

		public void LoadAnimation(FileInfo animationFile) {
			System.Diagnostics.Debug.Assert(animationFile != null, "FileInfo must not be null");
			if (!animationFile.Exists)
				throw new FileNotFoundException("File not found.", animationFile.Name);

			StreamReader data = animationFile.OpenText();
			try {
				List<AnimationData> animations = new List<AnimationData>(64);
				while (!data.EndOfStream) {
					string line = data.ReadLine().Trim();
				}
			} finally {
				data.Close();
				data.Dispose();
			}
		}
		/*
		private static bool _loadAnimationData(string asset,
										out Animation.AnimationData[] animations) {
			int frameHeight = 0;
			animations = null;
			try {
				int frameWidth = 0;
				using (StreamReader _animFile = File.OpenText(asset)) {
					List<Animation.AnimationData> _anims = new List<Animation.AnimationData>();
					while (!_animFile.EndOfStream) {
						string line = _animFile.ReadLine().Trim();
						if (line.ToLower().Contains("frameheight")) {
							string[] s = line.Split(new char[] { '=' });
							frameHeight = Convert.ToInt32(s[1]);
						} else if (line.ToLower().Contains("framewidth")) {
							string[] s = line.Split(new char[] { '=' });
							frameWidth = Convert.ToInt32(s[1]);
						} else if (line.Contains("[Animation]")) {
							var animation = new Animation.AnimationData();
							animation.FrameWidth = frameWidth;
							animation.FrameHeight = frameHeight;
							line = _animFile.ReadLine().Trim();
							while (!_animFile.EndOfStream && !line.Contains("[End]")) {
								if (line.ToLower().Contains("framewidth")) {
									string[] s = line.Split(new char[] { '=' });
									animation.FrameWidth = Convert.ToInt32(s[1]);
								} else if (line.ToLower().Contains("framecount")) {
									string[] s = line.Split(new char[] { '=' });
									animation.FrameCount = Convert.ToInt32(s[1]);
								} else if (line.ToLower().Contains("duration")) {
									string[] s = line.Split(new char[] { '=' });
									animation.Duration = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(s[1])).Ticks;
								}
								line = _animFile.ReadLine().Trim();
							}
							_anims.Add(animation);
						}
					}
					_animFile.Close();
					animations = _anims.ToArray();
				}
				return true;
			} catch (FileNotFoundException ex) {
				Console.Error.WriteLine(ex.Message);
			} catch (IndexOutOfRangeException ex) {
				Console.Error.WriteLine("Improperly formated .anim file.\n" + ex.Message);
			}
			return false;
		}
		 */
	}
}
