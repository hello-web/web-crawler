﻿using MovieCrawler.ApplicationServices;
using MovieCrawler.Core;
using MovieCrawler.Domain;
using MovieCrawler.Domain.Collections;
using MovieCrawler.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MovieCrawler.Domain.Data;

namespace MovieCrawler.ApplicationServices.MovieProviders
{
    public class FilmeOnline2013MovieProvider : IMovieProvider
    {
        private const string PageUriFormat = "http://www.filmeonline2013.biz/page/{0}/";

        public string Name { get { return "TopFilme2013.net"; } }

        public Uri Uri { get; private set; }

        public FilmeOnline2013MovieProvider()
        {
        }

        public IPageSet EnumerateFromPage(int startPage)
        {
            return new Enumerator(startPage);
        }

        public Task AddToBuilder(MovieBuilder builder, BasicMovieInfo movie)
        {
            var movieInfo = movie as SummaryMovieInfo;
            if (movieInfo == null)
                throw new ArgumentException("");

            return Build(builder, movie.Link);
        }

        public void AppendTo(MovieBuilder builder, PageInspectSubscription subscription)
        {
            throw new NotImplementedException();
        }

        private async Task Build(MovieBuilder builder, Uri uri)
        {
            var html = await WebHttp.GetHtmlDocument(uri);

            var entryContent = html.DocumentNode.SelectSingleNode("//div[@class='entry entry-content']")
                                                .ThrowExceptionIfNotExists("Unable to find the movie details element");
            var img = entryContent.SelectSingleNode("a[@class='entry-thumb']/img").ThrowExceptionIfNotExists("Movie thumbnail element");
            var categories = entryContent.SelectNodes("a[@rel='category tag']/text()")
                                         .ThrowExceptionIfNotExists("Unable to find the categories element")
                                         .Select(li => HttpUtility.HtmlDecode(li.InnerText));

            var movieInfo = builder.MovieInfo;
            movieInfo.LoadGenresFrom(categories);
            movieInfo.CoverImage = new Uri(img.GetAttributeValue("src", null));

            var description = entryContent.SelectSingleNode("p").ThrowExceptionIfNotExists("Unable to find description element");
            movieInfo.Description = HttpUtility.HtmlDecode(description.InnerText);
            
            foreach (var entry in html.DocumentNode.SelectNodes("//div[@class='entry-embed']/iframe")
                            .ThrowExceptionIfNotExists("Unable to find the movie streams"))
            {
                builder.Enqueue(HtmlHelpers.GetEmbededUri(entry));
            }
        }

        private static BasicMovieInfo CreateMovieInfoSummary(string link, string title)
        {
            if (string.IsNullOrEmpty(title))
                throw new InvalidDOMStructureException("Empty title");
            if (string.IsNullOrEmpty(link))
                throw new InvalidDOMStructureException("Empty link");

            return new BasicMovieInfo(title, new Uri(link));
        }

        class SummaryMovieInfo : BasicMovieInfo
        {
            public SummaryMovieInfo(string title, Uri uri) : base(title, uri)
            {
            }
        }

        class Enumerator : SyncronizedEnumerator
        {
            public Enumerator(int? startPage) 
                : base(startPage)
            {
            }

            protected override async Task<ICollection<BasicMovieInfo>> ParseFirstPage()
            {
                var html = await WebHttp.GetHtmlDocument(new Uri(string.Format(PageUriFormat, CurrentPage)));

                var movies = GetTopMovies(html);
                foreach (var movie in GetPaggedMovies(html))
                    movies.Add(movie);

                var pagesElement = html.DocumentNode.SelectSingleNode("//div[@class='wp-pagenavi']/span")
                                                            .ThrowExceptionIfNotExists("Unable to find the pages number element");

                var pagesMatch = SharedRegex.EnglishPageMatchRegex.Match(pagesElement.InnerText);
                if (!pagesMatch.Success)
                    throw new InvalidParseElementException("Unable to determine the pages count");

                SetTotalPages(int.Parse(pagesMatch.Groups[2].Value));
                return movies;
            }

            protected override async Task<ICollection<BasicMovieInfo>> ParsePage(int page)
            {
                var html = await WebHttp.GetHtmlDocument(new Uri(string.Format(PageUriFormat, CurrentPage)));
                return GetPaggedMovies(html);
            }

            private ICollection<BasicMovieInfo> GetTopMovies(HtmlAgilityPack.HtmlDocument html)
            {
                var list = new List<BasicMovieInfo>();
                foreach (var movie in html.DocumentNode.SelectNodes("//div[@class='smooth_slideri']/a"))
                {
                    var movieSummary = CreateMovieInfoSummary(movie.GetAttributeValue("href", null), movie.GetAttributeValue("title", null));
                    var img = movie.SelectSingleNode("img[@src]").ThrowExceptionIfNotExists("Tag 'img' not found");

                    list.Add(movieSummary);
                }
                return list;
            }

            private ICollection<BasicMovieInfo> GetPaggedMovies(HtmlAgilityPack.HtmlDocument html)
            {
                var list = new List<BasicMovieInfo>();
                foreach (var movie in html.DocumentNode.SelectNodes("//a[@class='entry-thumb2']"))
                {
                    var movieSummary = CreateMovieInfoSummary(movie.GetAttributeValue("href", null), movie.GetAttributeValue("title", null));
                    list.Add(movieSummary);
                }
                return list;
            }
        }
    }
}
