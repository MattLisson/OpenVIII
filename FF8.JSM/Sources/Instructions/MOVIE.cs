﻿using System;
using FF8.Core;
using FF8.Framework;
using FF8.JSM.Format;

namespace FF8.JSM.Instructions
{
    internal sealed class MOVIE : JsmInstruction
    {
        public MOVIE()
        {
        }

        public MOVIE(Int32 parameter, IStack<IJsmExpression> stack)
            : this()
        {
        }

        public override String ToString()
        {
            return $"{nameof(MOVIE)}()";
        }

        public override void Format(ScriptWriter sw, IScriptFormatterContext formatterContext, IServices services)
        {
            sw.Format(formatterContext, services)
                .StaticType(nameof(IMovieService))
                .Method(nameof(IMovieService.Play))
                .Comment(nameof(MOVIE));
        }

        public override IAwaitable TestExecute(IServices services)
        {
            ServiceId.Movie[services].Play();
            return DummyAwaitable.Instance;
        }
    }
}