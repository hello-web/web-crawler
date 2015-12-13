﻿using MovieCrawler.Domain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCrawler.Core;

namespace MovieCrawler.Domain
{
    public interface IMovieCrawler : ICrawler
    {
        void AppendTo(MovieBuilder builder, PageInspectSubscription subscription);
        //void BuildTo(MovieBuilder builder);
    }
}
