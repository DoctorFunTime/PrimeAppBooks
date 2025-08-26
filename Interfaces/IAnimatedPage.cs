using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Interfaces
{
    public interface IAnimatedPage
    {
        string AnimationStyle { get; } // "SlideFromRight", "SlideFromBottom", "FadeIn", etc.
    }
}