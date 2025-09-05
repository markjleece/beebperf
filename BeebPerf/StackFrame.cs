using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace BeebPerf
{
    public enum StackFrameType
    {
        ISR,
        JSR,
        TailCall,
        Unknown
    }
    
    public class StackFrame : IEquatable<StackFrame>
    {
        public StackFrame()
        {
            Routine = new Routine();
        }

        public StackFrame(Routine routine, StackFrameType type, StackFrame? parent)
        {
            Type = type;
            Parent = parent;
            Routine = routine;
            CanonicalAddress = routine.StartAddress;
        }

        public bool Equals(StackFrame? other)
        {
            if (other is null) return false;

            var self = this;
            var peer = other;

            while (self is not null && peer is not null)
            {
                if (!self.CanonicalAddress.Equals(peer.CanonicalAddress))
                    return false;

                if (self.Type == StackFrameType.ISR)
                    return true;

                self = self.Parent;
                peer = peer.Parent;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            for (var stackFrame = this; stackFrame != null; stackFrame = stackFrame.Parent) 
            {
                hashCode = unchecked(hashCode * 31 + stackFrame.CanonicalAddress.GetHashCode());
                if (stackFrame.Type == StackFrameType.ISR)
                    break;
            }
            return hashCode;
        }


        public readonly Routine Routine;
        public readonly CanonicalAddress CanonicalAddress;
        public readonly StackFrameType Type;
        public List<StackFrame> Children = new();
        public StackFrame? Parent;
        public int FirstInstructionIndex;
        public int LastInstructionIndex;
        public int StartCycleCount;
        public int EndCycleCount;
    }
}