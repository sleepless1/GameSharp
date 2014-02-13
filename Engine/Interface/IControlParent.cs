using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IControlParent : IControl {
		bool AddControl(IControl control);
		bool RemoveControl(IControl control);
	}
}
