/*
 * Copyright 2020 Magne Rasmussen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace CodeHive.DfaLex
{
    internal interface IRegexParserActions<T> where T : class
    {
        T Empty();

        T Literal(CharRange range);

        T Alternate(T p1, T p2);

        T Catenate(T p1, T p2);

        T Repeat(T p, int min = -1, int max = -1, bool greedy = true);

        T Group(T p, int no);
    }
}
