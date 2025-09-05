using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EquipmentWheel
{
    public interface IWheel
    {
        int GetKeyCountDown();
        int GetKeyCountPressed();
        bool IsVisible();
        void Hide();
        string GetName();
        float JoyStickIgnoreTime();
    }
}
