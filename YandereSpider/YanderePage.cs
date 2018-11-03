﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YandereSpider
{
    /// <summary>
    /// 提供获取 yande.re 页面的内含链接的方法。
    /// </summary>
    public class YanderePage : IDisposable, IReadOnlyList<YanderePage>, IEquatable<YanderePage>
    {
        /// <summary>
        /// yande.re 首页链接，默认跳转到 Posts 页面。
        /// </summary>
        public static readonly string IndexPageLink;
        /// <summary>
        /// Posts 页面链接。
        /// </summary>
        public static readonly string PostsPageLink;
        /// <summary>
        /// Pools 页面链接。
        /// </summary>
        public static readonly string PoolsPageLink;
        /// <summary>
        /// Post 页面链接的静态部分。
        /// </summary>
        protected static readonly string PostPageLinkStatic;
        /// <summary>
        /// Pool 页面链接的静态部分。
        /// </summary>
        protected static readonly string PoolPageLinkStatic;
        /// <summary>
        /// 图片链接前缀。
        /// </summary>
        protected static readonly string ImageLinkPrefix;
        /// <summary>
        /// Pool 页面链接前缀。
        /// </summary>
        protected static readonly string PoolPageLinkPrefix;
        /// <summary>
        /// 上一页链接前缀。
        /// </summary>
        protected static readonly string PrevPageLinkPrefix;
        /// <summary>
        /// 下一页链接前缀。
        /// </summary>
        protected static readonly string NextPageLinkPrefix;

        /// <summary>
        /// 获取 HTML 文本的任务。
        /// </summary>
        private Task<string> documentTextTask;
        /// <summary>
        /// 获取 HTML 文本的任务的取消标志信号源。
        /// </summary>
        private CancellationTokenSource documentCancellation;

        /// <summary>
        /// 初始化 <see cref="YanderePage"/> 类的静态成员。
        /// </summary>
        static YanderePage()
        {
            YanderePage.IndexPageLink = "https://yande.re";
            YanderePage.PostsPageLink = YanderePage.IndexPageLink + "/post";
            YanderePage.PoolsPageLink = YanderePage.IndexPageLink + "/pool";
            YanderePage.PostPageLinkStatic = YanderePage.PostsPageLink + "/show";
            YanderePage.PoolPageLinkStatic = YanderePage.PoolsPageLink + "/show";
            YanderePage.ImageLinkPrefix = "\"file_url\":\"";
            YanderePage.PoolPageLinkPrefix = "<a href=\"/pool/show";
            YanderePage.PrevPageLinkPrefix = "<a class=\"previous_page\" rel=\"prev\" href=\"";
            YanderePage.NextPageLinkPrefix = "<a class=\"next_page\" rel=\"next\" href=\"";
        }

        /// <summary>
        /// 使用 yande.re 首页的链接初始化 <see cref="YanderePage"/> 类的新实例。
        /// </summary>
        public YanderePage() : this(YanderePage.IndexPageLink) { }

        /// <summary>
        /// 使用页面的链接初始化 <see cref="YanderePage"/> 类的新实例。
        /// </summary>
        /// <param name="pageLink">页面的链接。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pageLink"/> 为 <see langword="null"/>。</exception>
        public YanderePage(string pageLink) : this(pageLink, true) { }

        /// <summary>
        /// 使用页面的链接和 HTML 文本初始化 <see cref="YanderePage"/> 类的新实例。
        /// </summary>
        /// <param name="pageLink">页面的链接。</param>
        /// <param name="documentText">页面的 HTML 文本。</param>
        /// <exception cref="ArgumentNullException"><paramref name="pageLink"/> 或
        /// <paramref name="documentText"/> 为 <see langword="null"/>。</exception>
        public YanderePage(string pageLink, string documentText) : this(pageLink, false)
        {
            if (documentText is null)
            {
                throw new ArgumentNullException(nameof(documentText));
            }
            
            this.documentTextTask = new Task<string>(() => documentText);
            this.documentTextTask.Start();
        }

        /// <summary>
        /// 使用页面的链接初始化 <see cref="YanderePage"/> 类的新实例，
        /// 并指定是否获取页面的 HTML 文本。
        /// </summary>
        /// <remarks>
        /// 若在派生类的初始化方法中调用了此初始化方法，
        /// 应重写 <see cref="YanderePage.DocumentText"/> 属性，
        /// 以确保基类能够正确读取页面的 HTML 文本并获取链接。
        /// </remarks>
        /// <param name="pageLink">页面的链接。</param>
        /// <param name="getsDocument">指示是否获取页面的 HTML 文本。</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pageLink"/> 为 <see langword="null"/>。</exception>
        protected YanderePage(string pageLink, bool getsDocument)
        {
            if (pageLink is null)
            {
                throw new ArgumentNullException(nameof(pageLink));
            }

            this.PageLink = pageLink;
            this.documentTextTask = getsDocument ?
                new HttpClient().GetStringAsync(pageLink) : null;
        }

        /// <summary>
        /// 获取当前页面能够直接导航的页面中指定索引处的页面。
        /// </summary>
        /// <param name="index">页面的索引，0 代表当前页面；
        /// 除此之外的索引应在 1 和 <see cref="YanderePage.Count"/> 之间。</param>
        /// <returns>当前页面能够直接导航的页面中指定索引处的页面，
        /// <paramref name="index"/> 为 0 时则为当前页面。</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> 超出允许的范围。</exception>
        public YanderePage this[int index]
        {
            get
            {
                if ((index < 0) || (index > this.Count))
                {
                    throw new IndexOutOfRangeException();
                }
                else if (index == 0)
                {
                    return this;
                }

                string pageIndexPrefix = "page=";
                string pageLink = (this.PageLink == YanderePage.IndexPageLink) ?
                    YanderePage.PostsPageLink : this.PageLink;

                string indexPageLink;
                if (pageLink.Contains(pageIndexPrefix))
                {
                    int pageIndexLength = 0;
                    int startIndex = pageLink.IndexOf(pageIndexPrefix) + pageIndexPrefix.Length;
                    for (int i = startIndex; i < pageLink.Length; i++)
                    {
                        if (char.IsDigit(pageLink[i]))
                        {
                            pageIndexLength++;
                        }
                        else { break; }
                    }
                    indexPageLink = pageLink.Remove(startIndex, pageIndexLength).
                        Insert(startIndex, index.ToString());
                }
                else
                {
                    string paramModifier = pageLink.Contains("?") ? "&" : "?";
                    indexPageLink = pageLink + paramModifier + pageIndexPrefix + index.ToString();
                }
                return new YanderePage(indexPageLink);
            }
        }

        /// <summary>
        /// 页面的链接。
        /// </summary>
        public string PageLink { get; }

        /// <summary>
        /// 页面的 HTML 文本。
        /// </summary>
        public virtual string DocumentText
        {
            get
            {
                if (this.documentTextTask is null) { return string.Empty; }

                try
                {
                    this.documentCancellation = new CancellationTokenSource();
                    this.documentTextTask.Wait(this.documentCancellation.Token);
                    return this.documentTextTask.Result;
                }
                catch (OperationCanceledException)
                {
                    this.documentTextTask = null;
                    this.documentCancellation = null;
                    return string.Empty;
                }
                catch (Exception)
                {
                    this.Refresh();
                    return this.DocumentText;
                }
            }
        }

        /// <summary>
        /// 当前页面的索引。
        /// </summary>
        public int Index
        {
            get
            {
                string pageIndexPrefix = "page=";
                string pageLink = this.PageLink;

                if (!pageLink.Contains(pageIndexPrefix)) { return 1; }

                string pageIndexString = string.Empty;
                int startIndex = pageLink.IndexOf(pageIndexPrefix) + pageIndexPrefix.Length;
                for (int i = startIndex; i < pageLink.Length; i++)
                {
                    if (char.IsDigit(pageLink[i]))
                    {
                        pageIndexString += pageLink[i];
                    }
                    else { break; }
                }
                return int.Parse(pageIndexString);
            }
        }

        /// <summary>
        /// 当前页面能够直接导航的页面数量。
        /// </summary>
        public int Count
        {
            get
            {
                string documentText = this.DocumentText;

                if (!documentText.Contains(YanderePage.NextPageLinkPrefix))
                {
                    return documentText.Contains(YanderePage.PrevPageLinkPrefix) ?
                        this.Index : 1;
                }

                string lastPageIndexString = string.Empty;
                int endIndex = documentText.IndexOf(YanderePage.NextPageLinkPrefix);
                for (int i = endIndex; i >= 0; i--)
                {
                    if (char.IsDigit(documentText[i]))
                    {
                        lastPageIndexString = documentText[i] + lastPageIndexString;
                    }
                    else if (lastPageIndexString.Length > 0) { break; }
                }
                return int.Parse(lastPageIndexString);
            }
        }

        /// <summary>
        /// 页面包含的图片链接。
        /// </summary>
        public string[] ImageLinks
        {
            get
            {
                string documentText = this.DocumentText;

                var imageLinks = new List<string>();
                while (documentText.Contains(YanderePage.ImageLinkPrefix))
                {
                    int startIndex = documentText.IndexOf(YanderePage.ImageLinkPrefix) +
                        YanderePage.ImageLinkPrefix.Length;
                    string imageLink = documentText.Substring(startIndex);
                    imageLink = imageLink.Remove(imageLink.IndexOf('"'));
                    imageLinks.Add(imageLink);
                    documentText = documentText.Substring(startIndex);
                }
                return imageLinks.ToArray();
            }
        }

        /// <summary>
        /// Pools 页面的所有 Pool 页面的链接。
        /// </summary>
        public string[] PoolPageLinks
        {
            get
            {
                string documentText = this.DocumentText;

                var poolPageLinks = new List<string>();
                while (documentText.Contains(YanderePage.PoolPageLinkPrefix))
                {
                    int startIndex = documentText.IndexOf(YanderePage.PoolPageLinkPrefix) +
                        YanderePage.PoolPageLinkPrefix.Length;
                    string poolPageLink = documentText.Substring(startIndex);
                    poolPageLink = poolPageLink.Remove(poolPageLink.IndexOf('"'));
                    poolPageLinks.Add(YanderePage.PoolPageLinkStatic + poolPageLink);
                    documentText = documentText.Substring(startIndex);
                }
                return poolPageLinks.ToArray();
            }
        }

        /// <summary>
        /// Pools 页面的所有 Pool 页面的 <see cref="YanderePage"/> 对象。
        /// </summary>
        public YanderePage[] PoolPages
        {
            get
            {
                var poolPages = new YanderePage[this.PoolPageLinks.Length];
                for (int i = 0; i < this.PoolPageLinks.Length; i++)
                {
                    poolPages[i] = new YanderePage(this.PoolPageLinks[i]);
                }
                return poolPages;
            }
        }

        /// <summary>
        /// 上一页面的链接。
        /// </summary>
        public string PrevPageLink
        {
            get
            {
                string documentText = this.DocumentText;

                if (!documentText.Contains(YanderePage.PrevPageLinkPrefix)) { return null; }

                int startIndex = documentText.IndexOf(YanderePage.PrevPageLinkPrefix) +
                    YanderePage.PrevPageLinkPrefix.Length;
                string prevPageLink = documentText.Substring(startIndex);
                prevPageLink = prevPageLink.Remove(prevPageLink.IndexOf('"'));
                return YanderePage.IndexPageLink + prevPageLink;
            }
        }

        /// <summary>
        /// 上一页面的 <see cref="YanderePage"/> 对象。
        /// </summary>
        public YanderePage PrevPage =>
            (this.PrevPageLink is null) ? null : new YanderePage(this.PrevPageLink);

        /// <summary>
        /// 下一页面的链接。
        /// </summary>
        public string NextPageLink
        {
            get
            {
                string documentText = this.DocumentText;

                if (!documentText.Contains(YanderePage.NextPageLinkPrefix)) { return null; }

                int startIndex = documentText.IndexOf(YanderePage.NextPageLinkPrefix) +
                    YanderePage.NextPageLinkPrefix.Length;
                string nextPageLink = documentText.Substring(startIndex);
                nextPageLink = nextPageLink.Remove(nextPageLink.IndexOf('"'));
                return YanderePage.IndexPageLink + nextPageLink;
            }
        }

        /// <summary>
        /// 下一页面的 <see cref="YanderePage"/> 对象。
        /// </summary>
        public YanderePage NextPage =>
            (this.NextPageLink is null) ? null : new YanderePage(this.NextPageLink);

        /// <summary>
        /// 指示页面是否为 yande.re 的页面。
        /// </summary>
        /// <param name="page">待验证的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>若 <paramref name="page"/> 为 yande.re 的页面，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public static bool IsYanderePage(YanderePage page) =>
            page.PageLink.StartsWith(YanderePage.IndexPageLink);

        /// <summary>
        /// 指示页面是否为 Posts 页面。
        /// </summary>
        /// <param name="page">待验证的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>若 <paramref name="page"/> 为 Posts 页面，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public static bool IsPostsPage(YanderePage page) =>
            page.PageLink.StartsWith(YanderePage.PostsPageLink) &&
            !page.PageLink.StartsWith(YanderePage.PostPageLinkStatic);

        /// <summary>
        /// 指示页面是否为 Pools 页面。
        /// </summary>
        /// <param name="page">待验证的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>若 <paramref name="page"/> 为 Pools 页面，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public static bool IsPoolsPage(YanderePage page) =>
            page.PageLink.StartsWith(YanderePage.PoolsPageLink) &&
            !page.PageLink.StartsWith(YanderePage.PoolPageLinkStatic);

        /// <summary>
        /// 指示页面是否为 Post 页面。
        /// </summary>
        /// <param name="page">待验证的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>若 <paramref name="page"/> 为 Post 页面，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public static bool IsPostPage(YanderePage page) =>
            page.PageLink.StartsWith(YanderePage.PostPageLinkStatic);

        /// <summary>
        /// 指示页面是否为 Pool 页面。
        /// </summary>
        /// <param name="page">待验证的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>若 <paramref name="page"/> 为 Pool 页面，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
        public static bool IsPoolPage(YanderePage page) =>
            page.PageLink.StartsWith(YanderePage.PoolPageLinkStatic);

        /// <summary>
        /// 重新获取当前页面的 HTML 文本以刷新内容。
        /// </summary>
        public virtual void Refresh() =>
            this.documentTextTask = new HttpClient().GetStringAsync(this.PageLink);

        /// <summary>
        /// 立刻停止所有 HTTP 传输任务，并释放此实例占用的资源。
        /// </summary>
        public virtual void Dispose() => this.documentCancellation?.Cancel();

        /// <summary>
        /// 指示当前实例与指定 <see cref="YanderePage"/> 对象是否相等。
        /// </summary>
        /// <param name="other">用于比较的 <see cref="YanderePage"/> 对象。</param>
        /// <returns>
        /// 若此实例与 <paramref name="other"/> 的 <see cref="YanderePage.PageLink"/> 相等，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        public bool Equals(YanderePage other) =>
            !(other is null) && (this.PageLink == other.PageLink);

        /// <summary>
        /// 指示当前实例与指定对象是否相等。
        /// </summary>
        /// <param name="obj">用于比较的对象。</param>
        /// <returns>
        /// 若 <paramref name="obj"/> 为 <see cref="YanderePage"/> 类型的对象，
        /// 且此实例与 <paramref name="obj"/> 的 <see cref="YanderePage.PageLink"/> 相等，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        public override bool Equals(object obj) => this.Equals(obj as YanderePage);

        /// <summary>
        /// 获取当前页面的哈希代码。
        /// </summary>
        /// <returns>32 位有符号整数的哈希代码。</returns>
        public override int GetHashCode() =>
            -76736031 + EqualityComparer<string>.Default.GetHashCode(this.PageLink);

        /// <summary>
        /// 返回当前页面的链接。
        /// </summary>
        /// <returns>当前页面的链接。</returns>
        public override string ToString() => this.PageLink.ToString();

        /// <summary>
        /// 返回一个从当前页面直到最后一页的枚举器。
        /// </summary>
        /// <returns>用于循环访问页面的枚举数。</returns>
        public IEnumerator<YanderePage> GetEnumerator()
        {
            for (var page = this; !(page is null); page = page.NextPage)
            {
                yield return page;
            }
        }

        /// <summary>
        /// 返回一个从当前页面直到最后一页的枚举器。
        /// </summary>
        /// <returns>用于循环访问页面的枚举数。</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// 指示两 <see cref="YanderePage"/> 对象是否相等。
        /// </summary>
        /// <param name="page1">第一个对象。</param>
        /// <param name="page2">第二个对象。</param>
        /// <returns>
        /// 若 <paramref name="page1"/> 与 <paramref name="page2"/> 的
        /// <see cref="YanderePage.PageLink"/> 相等，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        public static bool operator ==(YanderePage page1, YanderePage page2) =>
            EqualityComparer<YanderePage>.Default.Equals(page1, page2);

        /// <summary>
        /// 指示两 <see cref="YanderePage"/> 对象是否不相等。
        /// </summary>
        /// <param name="page1">第一个对象。</param>
        /// <param name="page2">第二个对象。</param>
        /// <returns>
        /// 若 <paramref name="page1"/> 与 <paramref name="page2"/> 的
        /// <see cref="YanderePage.PageLink"/> 不相等，
        /// 则为 <see langword="true"/>；否则为 <see langword="false"/>。
        /// </returns>
        public static bool operator !=(YanderePage page1, YanderePage page2) =>
            !(page1 == page2);
    }
}
