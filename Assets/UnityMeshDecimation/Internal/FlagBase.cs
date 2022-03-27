namespace UnityMeshDecimation.Internal {
	public class FlagBase {
		protected int flags = 0;

		public void SetFlags(int flags) {
			this.flags = flags;
		}

		public void ClearFlags() {
			this.flags = 0;
		}

		public bool HasFlag(int flag) {
			return (this.flags & flag) == flag;
		}

		public void AddFlag(int flag) {
			this.flags |= flag;
		}

		public void RemoveFlag(int flag) {
			this.flags &= ~flag;
		}
	}
}
