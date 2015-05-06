﻿/** 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *         http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Difi.SikkerDigitalPost.Klient.Domene.Enums
{
    public enum Feiltype
    {
        /// <summary>
        /// Feil som har oppstått som følge av en feil hos klienten.
        /// </summary>
        Klient,
        /// <summary>
        /// Feil som har oppstått som følge av feil på sentral infrastruktur. Bør meldes til sentralforvalter.
        /// </summary>
        Server
    }
}
