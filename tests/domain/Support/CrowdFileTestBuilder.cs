using Module.HeroVirtualTabletop.Crowds;
using System;
using System.Collections.Generic;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Fluent builder that constructs a List&lt;CrowdModel&gt; suitable for JSON serialisation
    /// as a crowd file on disk.  Use in <c>given_crowd_file_on_disk</c> helpers.
    /// </summary>
    public class CrowdFileTestBuilder
    {
        private readonly List<CrowdModel> _topLevels = new List<CrowdModel>();

        public CrowdTestBuilder TopLevel(string name)
        {
            CrowdModel crowd = new CrowdModel { Name = name };
            _topLevels.Add(crowd);
            return new CrowdTestBuilder(crowd, this);
        }

        public List<CrowdModel> Build()
        {
            return _topLevels;
        }
    }

    public class CrowdTestBuilder
    {
        private readonly CrowdModel _crowd;
        private readonly CrowdFileTestBuilder _file;

        public CrowdTestBuilder(CrowdModel crowd, CrowdFileTestBuilder file)
        {
            _crowd = crowd;
            _file = file;
        }

        public CrowdTestBuilder WithCharacter(string name)
        {
            _crowd.Add(new CrowdMemberModel { Name = name });
            return this;
        }

        public CrowdTestBuilder WithNested(string nestedName, Action<CrowdTestBuilder> build)
        {
            CrowdModel nested = new CrowdModel { Name = nestedName };
            _crowd.Add(nested);
            build(new CrowdTestBuilder(nested, _file));
            return this;
        }

        public CrowdTestBuilder TopLevel(string name)
        {
            return _file.TopLevel(name);
        }

        public List<CrowdModel> Build()
        {
            return _file.Build();
        }
    }
}
